namespace DevCrew.Core.Services;

/// <summary>
/// Uygulamanın başlatılması için gerekli işlemleri yönetir
/// </summary>
public interface IApplicationService
{
    /// <summary>
    /// Uygulamayı başlat
    /// </summary>
    /// <param name="cancellationToken">İptal tokeni</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uygulamayı kapat
    /// </summary>
    /// <param name="cancellationToken">İptal tokeni</param>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// ApplicationService varsayılan uygulaması
/// </summary>
public class ApplicationService : IApplicationService
{
    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Uygulama başlatma işlemleri burada yapılacak
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        // Uygulama kapatma işlemleri burada yapılacak
        return Task.CompletedTask;
    }
}
