namespace DevCrew.Desktop.Services;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    Task StartUpdateAsync(string latestVersion, CancellationToken cancellationToken = default);
}
