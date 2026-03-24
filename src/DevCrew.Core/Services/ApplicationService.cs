namespace DevCrew.Core.Services;

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
        // Application shutdown operations will be performed here
        return Task.CompletedTask;
    }
}

