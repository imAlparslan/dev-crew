using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the GUID creation view
/// Each tab instance maintains its own state
/// </summary>
public partial class CreateGuidViewModel : ObservableObject
{
    private readonly IGuidService _guidService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGuid))]
    private string currentGuid = string.Empty;
    public bool HasGuid => !string.IsNullOrWhiteSpace(CurrentGuid);

    [ObservableProperty]
    private bool isGuidCopied;

    [ObservableProperty]
    private IBrush guidBackground;

    [ObservableProperty]
    private IBrush guidBorderBrush;

    private static readonly IBrush FallbackTertiaryBackground = new SolidColorBrush(Color.Parse("#334155"));
    private static readonly IBrush FallbackBorderSecondary = new SolidColorBrush(Color.Parse("#475569"));
    private static readonly IBrush FallbackAccent = new SolidColorBrush(Color.Parse("#0EA5E9"));
    private static readonly IBrush FallbackAccentDark = new SolidColorBrush(Color.Parse("#0284C7"));

    public CreateGuidViewModel(IGuidService guidService)
    {
        _guidService = guidService;
        guidBackground = GetBrush("BrushTertiaryBackground", FallbackTertiaryBackground);
        guidBorderBrush = GetBrush("BrushBorderSecondary", FallbackBorderSecondary);
    }

    [RelayCommand]
    private void GenerateGuid()
    {
        CurrentGuid = _guidService.Generate();
    }

    [RelayCommand]
    private async Task CopyToClipboard()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel?.Clipboard != null && !string.IsNullOrWhiteSpace(CurrentGuid))
            {
                await topLevel.Clipboard.SetTextAsync(CurrentGuid);
                IsGuidCopied = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CurrentGuid = string.Empty;
    }

    partial void OnCurrentGuidChanged(string value)
    {
        IsGuidCopied = false;
        GuidBackground = GetBrush("BrushTertiaryBackground", FallbackTertiaryBackground);
        GuidBorderBrush = GetBrush("BrushBorderSecondary", FallbackBorderSecondary);
    }

    partial void OnIsGuidCopiedChanged(bool value)
    {
        if (value)
        {
            GuidBackground = GetBrush("BrushAccent", FallbackAccent);
            GuidBorderBrush = GetBrush("BrushAccentDark", FallbackAccentDark);
        }
        else
        {
            GuidBackground = GetBrush("BrushTertiaryBackground", FallbackTertiaryBackground);
            GuidBorderBrush = GetBrush("BrushBorderSecondary", FallbackBorderSecondary);
        }
    }

    private static IBrush GetBrush(string key, IBrush fallback)
    {
        if (Application.Current?.Resources != null
            && Application.Current.Resources.TryGetValue(key, out var resource)
            && resource is IBrush brush)
        {
            return brush;
        }

        return fallback;
    }
}
