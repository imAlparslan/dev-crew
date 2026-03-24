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
