using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Web;
using ServiceStack.Model;

namespace MyApp.ServiceInterface;

public interface IAsyncCommand<in T> : IAsyncCommand
{
    Task ExecuteAsync(T request);
}
public interface IAsyncCommand { }

public interface ICommandExecutor
{
    TCommand Command<TCommand>() where TCommand : IAsyncCommand;
    Task ExecuteAsync<TRequest>(IAsyncCommand<TRequest> command, TRequest request);
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class CommandAttribute(Type commandType, Lifetime lifetime = Lifetime.Transient) : AttributeBase
{
    public Type CommandType { get; } = commandType;
    public Lifetime Lifetime { get; } = lifetime;
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class CommandAttribute<T>(Lifetime lifetime = Lifetime.Transient) 
    : CommandAttribute(typeof(T), lifetime) where T : IAsyncCommand;

public enum Lifetime
{
    /// <summary>
    /// Specifies that a single instance of the service will be created.
    /// </summary>
    Singleton,
    
    /// <summary>
    /// Specifies that a new instance of the service will be created for each scope.
    /// </summary>
    /// <remarks>
    /// In ASP.NET Core applications a scope is created around each server request.
    /// </remarks>
    Scoped,
    
    /// <summary>
    /// Specifies that a new instance of the service will be created every time it is requested.
    /// </summary>
    Transient,
}

public class CommandsFeature : IPlugin, IConfigureServices, IHasStringId
{
    public string Id => "commands";

    public const int DefaultCapacity = 250;
    public int ResultsCapacity { get; set; } = DefaultCapacity;
    public int FailuresCapacity { get; set; } = DefaultCapacity;
    public int TimingsCapacity { get; set; } = 1000;
    
    /// <summary>
    /// Ignore commands or Request DTOs from being logged
    /// </summary>
    public List<string> Ignore { get; set; } = [nameof(ViewCommands)];
    
    public Func<CommandResult,bool>? ShouldIgnore { get; set; }

    /// <summary>
    /// Limit API access to users in role
    /// </summary>
    public string AccessRole { get; set; } = RoleNames.Admin;

    public Dictionary<Type, string[]> ServiceRoutes { get; set; } = new() {
        [typeof(ViewCommandsService)] = ["/" + "commands".Localize()],
    };

    public List<(Type, ServiceLifetime)> RegisterTypes { get; set; } =
    [
        (typeof(IDbConnection), ServiceLifetime.Transient),
        (typeof(IRedisClient), ServiceLifetime.Singleton),
        (typeof(IRedisClientAsync), ServiceLifetime.Singleton),
        (typeof(IMessageProducer), ServiceLifetime.Singleton),
    ];

    public void Configure(IServiceCollection services)
    {
        services.AddTransient<ICommandExecutor>(c => new CommandExecutor(this, c));

        ServiceLifetime ToServiceLifetime(Lifetime lifetime) => lifetime switch {
            Lifetime.Scoped => ServiceLifetime.Scoped,
            Lifetime.Singleton => ServiceLifetime.Singleton,
            _ => ServiceLifetime.Transient
        };
        
        foreach (var requestType in ServiceStackHost.InitOptions.ResolveAssemblyRequestTypes())
        {
            var requestProps = TypeProperties.Get(requestType).PublicPropertyInfos;
            foreach (var prop in requestProps)
            {
                var commandAttr = prop.GetCustomAttribute<CommandAttribute>();
                if (commandAttr == null)
                    continue;

                services.Add(commandAttr.CommandType, commandAttr.CommandType, ToServiceLifetime(commandAttr.Lifetime));
            }
        }

        foreach (var registerType in RegisterTypes)
        {
            if (registerType.Item1 == typeof(IDbConnection) && !services.Exists<IDbConnection>())
            {
                services.Add(registerType.Item1, _ => HostContext.AppHost.GetDbConnection(), registerType.Item2);
            }
            if (registerType.Item1 == typeof(IRedisClient) && !services.Exists<IRedisClient>())
            {
                services.Add(registerType.Item1, _ => HostContext.AppHost.GetRedisClient(), registerType.Item2);
            }
            if (registerType.Item1 == typeof(IRedisClientAsync) && !services.Exists<IRedisClientAsync>())
            {
                services.Add(registerType.Item1, _ => HostContext.AppHost.GetRedisClientAsync(), registerType.Item2);
            }
            if (registerType.Item1 == typeof(IMessageProducer) && !services.Exists<IMessageProducer>())
            {
                services.Add(registerType.Item1, _ => HostContext.AppHost.GetMessageProducer(), registerType.Item2);
            }
        }

        services.RegisterServices(ServiceRoutes);
    }

    private ILogger<CommandsFeature>? log;

    public void Register(IAppHost appHost)
    {
        // if (appHost is ServiceStackHost host)
        //     host.AddTimings = true;
        
        log = appHost.GetApplicationServices().GetRequiredService<ILogger<CommandsFeature>>();
    }

    class CommandExecutor(CommandsFeature feature, IServiceProvider services) : ICommandExecutor
    {
        public TCommand Command<TCommand>() where TCommand : IAsyncCommand => services.GetRequiredService<TCommand>();

        public Task ExecuteAsync<T>(IAsyncCommand<T> command, T request)
        {
            return feature.ExecuteCommandAsync(command, request);
        }
    }

    public Task ExecuteCommandAsync<TCommand, TRequest>(TCommand command, TRequest request) 
        where TCommand : IAsyncCommand<TRequest> 
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteCommandAsync(command.GetType(), dto => command.ExecuteAsync((TRequest)dto), request);
    }

    public async Task ExecuteCommandAsync(Type commandType, Func<object,Task> execFn, object requestDto)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await execFn(requestDto);
            log!.LogDebug("{Command} took {ElapsedMilliseconds}ms to execute", commandType.Name, sw.ElapsedMilliseconds);

            AddCommandResult(new()
            {
                Name = commandType.Name,
                Ms = sw.ElapsedMilliseconds,
                At = DateTime.UtcNow,
            });
        }
        catch (Exception e)
        {
            var requestBody = requestDto.ToSafeJson();
            log!.LogError(e, "{Command}({Request}) failed: {Message}", commandType.Name, requestBody, e.Message);

            AddCommandResult(new()
            {
                Name = commandType.Name,
                Ms = sw.ElapsedMilliseconds,
                At = DateTime.UtcNow,
                Request = requestBody,
                Error = e.ToResponseStatus(),
            });
        }
    }

    public async Task ExecuteCommandsAsync<T>(IServiceProvider services, T requestDto) where T : class
    {
        var obj = requestDto.ToObjectDictionary();
        foreach (var commandProp in TypeProperties.Get(typeof(T)).PublicPropertyInfos)
        {
            var commandType = commandProp.GetCustomAttribute<CommandAttribute>()?.CommandType;
            if (commandType == null)
                continue;
            if (!obj.TryGetValue(commandProp.Name, out var requestProp) || requestProp == null)
                continue;

            var oCommand = services.GetRequiredService(commandType);
            var method = commandType.GetMethod("ExecuteAsync")
                ?? throw new NotSupportedException("ExecuteAsync method not found on " + commandType.Name);
                
            async Task Exec(object commandArg)
            {
                var methodInvoker = GetInvoker(method);
                await methodInvoker(oCommand, commandArg);
            }

            await ExecuteCommandAsync(commandType, Exec, requestProp);
        }
    }
    
    public ConcurrentQueue<CommandResult> CommandResults { get; set; } = [];
    public ConcurrentQueue<CommandResult> CommandFailures { get; set; } = new();
    
    public ConcurrentDictionary<string, CommandSummary> CommandTotals { get; set; } = new();

    public void AddCommandResult(CommandResult result)
    {
        if (Ignore.Contains(result.Name))
            return;
        if (ShouldIgnore != null && ShouldIgnore(result))
            return;
        
        var ms = (int)(result.Ms ?? 0);
        if (result.Error == null)
        {
            CommandResults.Enqueue(result);
            while (CommandResults.Count > ResultsCapacity)
                CommandResults.TryDequeue(out _);

            CommandTotals.AddOrUpdate(result.Name, 
                _ => new CommandSummary
                {
                    Type = result.Type,
                    Name = result.Name, 
                    Count = 1, 
                    TotalMs = ms, 
                    MinMs = ms > 0 ? ms : int.MinValue, 
                    Timings = new([ms]),
                },
                (_, summary) => 
                {
                    summary.Count++;
                    summary.TotalMs += ms;
                    summary.MaxMs = Math.Max(summary.MaxMs, ms);
                    if (ms > 0)
                    {
                        summary.MinMs = Math.Min(summary.MinMs, ms);
                    }
                    summary.Timings.Enqueue(ms);
                    while (summary.Timings.Count > TimingsCapacity)
                        summary.Timings.TryDequeue(out var _);
                    return summary;
                });
        }
        else
        {
            CommandFailures.Enqueue(result);
            while (CommandFailures.Count > FailuresCapacity)
                CommandFailures.TryDequeue(out _);

            CommandTotals.AddOrUpdate(result.Name, 
                _ => new CommandSummary { Name = result.Name, Failed = 1, Count = 0, TotalMs = 0, MinMs = int.MinValue, LastError = result.Error?.Message },
                (_, summary) =>
                {
                    summary.Failed++;
                    summary.LastError = result.Error?.Message;
                    return summary;
                });
        }
    }

    public void AddRequest(object requestDto, object response, TimeSpan elapsed)
    {
        var name = requestDto.GetType().Name;
        if (Ignore.Contains(name))
            return;
        
        var ms = (int)elapsed.TotalMilliseconds;
        var error = response.GetResponseStatus();
        if (error == null)
        {
            AddCommandResult(new()
            {
                Type = "API",
                Name = name,
                Ms = ms,
                At = DateTime.UtcNow,
            });
        }
        else
        {
            AddCommandResult(new()
            {
                Type = "API",
                Name = name,
                Ms = ms,
                At = DateTime.UtcNow,
                Request = requestDto.ToSafeJson(),
                Error = error,
            });
        }
    }
    
    public delegate Task AsyncMethodInvoker(object instance, object arg);
    static readonly ConcurrentDictionary<MethodInfo, AsyncMethodInvoker> invokerCache = new();

    public static AsyncMethodInvoker GetInvokerToCache(MethodInfo method)
    {
        if (method.IsStatic)
            throw new NotSupportedException("Static Method not supported");
            
        var paramInstance = Expression.Parameter(typeof(object), "instance");
        var paramArg = Expression.Parameter(typeof(object), "arg");

        var convertFromMethod = typeof(ServiceStack.TypeExtensions).GetStaticMethod(nameof(ServiceStack.TypeExtensions.ConvertFromObject));

        var convertParam = convertFromMethod.MakeGenericMethod(method.GetParameters()[0].ParameterType);
        var paramTypeArg = Expression.Call(convertParam, paramArg); 

        var convertReturn = convertFromMethod.MakeGenericMethod(method.ReturnType);

        var methodCall = Expression.Call(Expression.TypeAs(paramInstance, method.DeclaringType!), method, paramTypeArg);

        var lambda = Expression.Lambda(typeof(AsyncMethodInvoker), 
            Expression.Call(convertReturn, methodCall), 
            paramInstance, 
            paramArg);

        var fn = (AsyncMethodInvoker)lambda.Compile();
        return fn;
    }

    /// <summary>
    /// Create an Invoker for public instance methods
    /// </summary>
    public AsyncMethodInvoker GetInvoker(MethodInfo method)
    {
        if (invokerCache.TryGetValue(method, out var fn))
            return fn;
        fn = GetInvokerToCache(method);
        invokerCache[method] = fn;
        return fn;
    }
}

public class CommandResult
{
    public string Type { get; set; } = "CMD";
    public string Name { get; set; }
    public long? Ms { get; set; }
    public DateTime At { get; set; }
    public string Request { get; set; }
    public ResponseStatus? Error { get; set; }

    public CommandResult Clone(Action<CommandResult>? configure = null) => X.Apply(new CommandResult
    {
        Type = Type,
        Name = Name,
        Ms = Ms,
        At = At,
        Request = Request,
        Error = Error,
    }, configure);
}

public class CommandSummary
{
    public string Type { get; set; } = "CMD";
    public string Name { get; set; }
    public int Count { get; set; }
    public int Failed { get; set; }
    public int TotalMs { get; set; }
    public int MinMs { get; set; }
    public int MaxMs { get; set; }
    public double AverageMs => Count == 0 ? 0 : Math.Round(TotalMs / (double)Count, 2);
    public double MedianMs => Math.Round(Timings.Median(), 2);
    public string? LastError { get; set; }
    public ConcurrentQueue<int> Timings { get; set; } = new();
}

[ExcludeMetadata]
public class ViewCommands : IGet, IReturn<ViewCommandsResponse>
{
    public List<string>? Include { get; set; }
}

public class ViewCommandsResponse
{
    public List<CommandSummary> CommandTotals { get; set; }
    public List<CommandResult> LatestCommands { get; set; }
    public List<CommandResult> LatestFailed { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[DefaultRequest(typeof(ViewCommands))]
public class ViewCommandsService : Service
{
    public async Task<object> Any(ViewCommands request)
    {
        var feature = HostContext.AssertPlugin<CommandsFeature>();
        if (!HostContext.DebugMode)
            await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AccessRole);

        var to = new ViewCommandsResponse
        {
            LatestCommands = new(feature.CommandResults),
            LatestFailed = new(feature.CommandFailures),
            CommandTotals = new(feature.CommandTotals.Values)
        };

        if (request.Include?.Contains(nameof(ResponseStatus.StackTrace)) != true)
        {
            to.LatestFailed = to.LatestFailed.Map(x => x.Clone(c =>
            {
                if (c.Error != null)
                    c.Error.StackTrace = null;
            }));
        }
        
        return to;
    }
}

public static class CommandExtensions
{
    public static Task ExecuteAsync<TCommand, TRequest>(this ICommandExecutor executor, TRequest request) where TCommand : IAsyncCommand<TRequest>
    {
        var command = executor.Command<TCommand>();
        return executor.ExecuteAsync(command, request);
    }

    public static Task ExecuteCommandsAsync<T>(this IRequest? req, T requestDto) where T : class
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(requestDto);
        
        var services = req.TryResolve<IServiceProvider>();
        if (services == null)
            throw new NotSupportedException(nameof(IServiceProvider) + " not available");
        var feature = HostContext.AssertPlugin<CommandsFeature>();
        return feature.ExecuteCommandsAsync(services, requestDto);
    }
    
    public static double Median(this IEnumerable<int> nums)
    {
        var array = nums.ToArray();
        if (array.Length == 0) return 0;
        if (array.Length == 1) return array[0];
        Array.Sort(array);
        var mid = Math.Min(array.Length / 2, array.Length - 1);
        return array.Length % 2 == 0 
            ? (array[mid] + array[mid - 1]) / 2.0 
            : array[mid];
    }    
}

