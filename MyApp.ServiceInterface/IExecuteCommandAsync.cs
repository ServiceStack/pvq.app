namespace MyApp.ServiceInterface;

public interface IExecuteCommandAsync<in T>
{
    Task ExecuteAsync(T request);
}