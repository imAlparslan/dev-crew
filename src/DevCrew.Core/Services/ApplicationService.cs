namespace DevCrew.Core.Services;

/// <summary>
/// Manages the operations required to initialize the application
/// </summary>
public interface IApplicationService
{
    /// <summary>
    /// Initialize the application
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Shutdown the application
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of ApplicationService
/// </summary>
public class ApplicationService : IApplicationService
{
    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Application initialization operations will be performed here
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        // Uygulama kapatma işlemleri burada yapılacak
        return Task.CompletedTask;
    }
}
