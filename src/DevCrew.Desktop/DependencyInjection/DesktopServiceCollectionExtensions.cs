using DevCrew.Core;
using DevCrew.Core.Application.Services;
using DevCrew.Desktop.Services;
using DevCrew.Desktop.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevCrew.Desktop.DependencyInjection;

public static class DesktopServiceCollectionExtensions
{
    public static IServiceCollection AddDesktopServices(this IServiceCollection services, IConfiguration? configuration)
    {
        var effectiveConfiguration = configuration ?? new ConfigurationBuilder().Build();

        services.AddSingleton(effectiveConfiguration);

        // Core Services
        services.AddDevCrewCore(effectiveConfiguration);

        // Desktop Services
        services.AddSingleton<ILocalizationService>(_ =>
            new LocalizationService(LocalizationService.ResolveOrFallbackCulture(System.Globalization.CultureInfo.CurrentUICulture.Name)));
        services.AddSingleton<IFontService, FontService>();
        services.AddSingleton<IUninstallService, UninstallService>();
        services.AddScoped<IClipboardService, ClipboardService>();

        // ViewModels
        services.AddScoped<MainWindowViewModel>();
        services.AddSingleton<DashboardViewModel>();

        services.AddTransient<CreateGuidViewModel>();
        services.AddTransient<Func<CreateGuidViewModel>>(sp => () => sp.GetRequiredService<CreateGuidViewModel>());

        services.AddTransient<JwtDecoderViewModel>();
        services.AddTransient<Func<JwtDecoderViewModel>>(sp => () => sp.GetRequiredService<JwtDecoderViewModel>());

        services.AddTransient<JwtBuilderViewModel>();
        services.AddTransient<Func<JwtBuilderViewModel>>(sp => () => sp.GetRequiredService<JwtBuilderViewModel>());

        services.AddTransient<JsonFormatterViewModel>();
        services.AddTransient<Func<JsonFormatterViewModel>>(sp => () => sp.GetRequiredService<JsonFormatterViewModel>());

        services.AddTransient<JsonDiffViewModel>();
        services.AddTransient<Func<JsonDiffViewModel>>(sp => () => sp.GetRequiredService<JsonDiffViewModel>());

        services.AddTransient<Base64EncoderViewModel>();
        services.AddTransient<Func<Base64EncoderViewModel>>(sp => () => sp.GetRequiredService<Base64EncoderViewModel>());

        services.AddTransient<Base64DecoderViewModel>();
        services.AddTransient<Func<Base64DecoderViewModel>>(sp => () => sp.GetRequiredService<Base64DecoderViewModel>());

        services.AddTransient<RegexViewModel>();
        services.AddTransient<Func<RegexViewModel>>(sp => () => sp.GetRequiredService<RegexViewModel>());

        services.AddTransient<SettingsViewModel>();
        services.AddTransient<Func<SettingsViewModel>>(sp => () => sp.GetRequiredService<SettingsViewModel>());

        return services;
    }
}
