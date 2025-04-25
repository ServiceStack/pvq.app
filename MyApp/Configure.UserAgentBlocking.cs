using Microsoft.Extensions.Options;

namespace MyApp;

/// <summary>
/// Options for configuring the UserAgentBlockingMiddleware
/// </summary>
public class UserAgentBlockingOptions
{
    /// <summary>
    /// List of user agents to block (supports exact matches or substring matches)
    /// </summary>
    public List<string> BlockedUserAgents { get; set; } = new();

    /// <summary>
    /// HTTP status code to return when a user agent is blocked (defaults to 403 Forbidden)
    /// </summary>
    public int BlockedStatusCode { get; set; } = StatusCodes.Status403Forbidden;

    /// <summary>
    /// Optional message to return in the response body when a user agent is blocked
    /// </summary>
    public string BlockedMessage { get; set; } = "Access denied based on your user agent";

    /// <summary>
    /// If true, will perform case-insensitive matching (defaults to true)
    /// </summary>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// If true, blocked requests will be logged (defaults to true)
    /// </summary>
    public bool LogBlockedRequests { get; set; } = true;
}

/// <summary>
/// Middleware that blocks requests from specific user agents
/// </summary>
public class UserAgentBlockingMiddleware(
    RequestDelegate next,
    IOptions<UserAgentBlockingOptions> options,
    ILogger<UserAgentBlockingMiddleware> logger)
{
    UserAgentBlockingOptions Options => options?.Value ?? throw new ArgumentNullException(nameof(options));
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Get the User-Agent header
        string userAgent = context.Request.Headers["User-Agent"].ToString();

        // Check if the user agent should be blocked
        if (ShouldBlockUserAgent(userAgent))
        {
            // Log the blocked request if enabled
            if (Options.LogBlockedRequests)
            {
                logger.LogInformation(
                    "Request blocked from user agent: {UserAgent}, IP: {IPAddress}, Path: {Path}",
                    userAgent,
                    context.Connection.RemoteIpAddress,
                    context.Request.Path);
            }

            // Set the response status code
            context.Response.StatusCode = Options.BlockedStatusCode;
            context.Response.ContentType = "text/plain";

            // Write the blocked message to the response
            await context.Response.WriteAsync(Options.BlockedMessage);
            return;
        }

        // If not blocked, continue to the next middleware
        await next(context);
    }

    private bool ShouldBlockUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            // You might want to block requests with no user agent
            // Return true here if you want to block empty user agents
            return false;
        }

        foreach (var blockedAgent in Options.BlockedUserAgents)
        {
            var comparison = Options.IgnoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (userAgent.Contains(blockedAgent, comparison))
                return true;
            if (blockedAgent.Contains(' ') && userAgent.Contains(blockedAgent.Replace(" ",""), comparison))
                return true;
        }

        return false;
    }
}

/// <summary>
/// Extension methods for registering the UserAgentBlockingMiddleware
/// </summary>
public static class UserAgentBlockingMiddlewareExtensions
{
    /// <summary>
    /// Adds the UserAgentBlockingMiddleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseUserAgentBlocking(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserAgentBlockingMiddleware>();
    }

    /// <summary>
    /// Adds the UserAgentBlockingMiddleware to the application pipeline with custom options
    /// </summary>
    public static IApplicationBuilder UseUserAgentBlocking(
        this IApplicationBuilder builder,
        Action<UserAgentBlockingOptions> configureOptions)
    {
        // Create a new options instance
        var options = new UserAgentBlockingOptions();

        // Apply the configuration
        configureOptions(options);

        // Use the middleware with the configured options
        return builder.UseMiddleware<UserAgentBlockingMiddleware>(Options.Create(options));
    }
}