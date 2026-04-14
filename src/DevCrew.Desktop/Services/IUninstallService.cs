namespace DevCrew.Desktop.Services;

public interface IUninstallService
{
    bool IsSupported { get; }
    Task UninstallAsync();
}
