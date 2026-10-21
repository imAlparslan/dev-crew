using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Application.Services;
using DevCrew.Desktop.Services;

namespace DevCrew.Desktop.ViewModels;

public sealed record McpClientOption(string Id, string DisplayName);

public partial class McpViewModel : BaseViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IClipboardService _clipboardService;
    private readonly ILocalizationService _localizationService;
    private string? _statusKey;
    private object[] _statusArguments = [];

    [ObservableProperty]
    private string executablePath = string.Empty;

    [ObservableProperty]
    private McpClientOption? selectedClient;

    [ObservableProperty]
    private string configurationText = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isError;

    public McpViewModel(
        IErrorHandler errorHandler,
        IClipboardService clipboardService,
        ILocalizationService localizationService)
        : base(errorHandler)
    {
        _clipboardService = clipboardService;
        _localizationService = localizationService;

        Clients = CreateClientOptions();
        SelectedClient = Clients[0];
        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    public IReadOnlyList<McpClientOption> Clients { get; private set; }

    public string Title => _localizationService.GetString("mcp.title");
    public string Description => _localizationService.GetString("mcp.description");
    public string ExecutableStatusLabel => _localizationService.GetString("mcp.executable_status");
    public string ClientLabel => _localizationService.GetString("mcp.client");
    public string TransportLabel => _localizationService.GetString("mcp.transport");
    public string TransportValue => _localizationService.GetString("mcp.stdio");
    public string ConfigurationLabel => _localizationService.GetString("mcp.configuration");
    public string CopyConfigurationButtonLabel => _localizationService.GetString("mcp.copy_configuration");
    public string ExecutableStatus => string.IsNullOrWhiteSpace(ExecutablePath)
        ? _localizationService.GetString("mcp.not_found")
        : _localizationService.GetString("mcp.detected", ExecutablePath);
    public bool IsConfigurationAvailable => !string.IsNullOrWhiteSpace(ExecutablePath) && SelectedClient is not null;

    partial void OnExecutablePathChanged(string value)
    {
        OnPropertyChanged(nameof(ExecutableStatus));
        OnPropertyChanged(nameof(IsConfigurationAvailable));
        RefreshConfiguration();
        CopyConfigurationCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedClientChanged(McpClientOption? value)
    {
        RefreshConfiguration();
        CopyConfigurationCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private Task InitializeAsync()
    {
        ExecutablePath = McpExecutableLocator.Locate() ?? string.Empty;
        SetStatus(ExecutablePath.Length == 0 ? "mcp.not_found_status" : "mcp.detected_status");
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanCopyConfiguration))]
    private async Task CopyConfigurationAsync()
    {
        try
        {
            var copied = await _clipboardService.TrySetTextAsync(ConfigurationText);
            SetStatus(copied ? "mcp.configuration_copied" : "mcp.copy_failed", isError: !copied);
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, "Copy MCP configuration");
            SetStatus("mcp.copy_failed", ex.Message, isError: true);
        }
    }

    private bool CanCopyConfiguration()
        => IsConfigurationAvailable && !string.IsNullOrWhiteSpace(ConfigurationText);

    private void RefreshConfiguration()
    {
        ConfigurationText = IsConfigurationAvailable
            ? BuildConfiguration(SelectedClient!.Id, ExecutablePath)
            : string.Empty;
    }

    private static string BuildConfiguration(string clientId, string executablePath)
    {
        var server = new Dictionary<string, object>
        {
            ["command"] = executablePath,
            ["args"] = Array.Empty<string>()
        };

        object configuration = clientId switch
        {
            "vscode_copilot" => new
            {
                servers = new Dictionary<string, object>
                {
                    ["devcrew"] = new Dictionary<string, object>(server)
                    {
                        ["type"] = "stdio"
                    }
                }
            },
            "claude_desktop" or "cursor" => new
            {
                mcpServers = new Dictionary<string, object>
                {
                    ["devcrew"] = server
                }
            },
            "generic_stdio" => new
            {
                type = "stdio",
                command = executablePath,
                args = Array.Empty<string>()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(clientId), clientId, "Unsupported MCP client.")
        };

        return JsonSerializer.Serialize(configuration, configuration.GetType(), JsonOptions);
    }

    private IReadOnlyList<McpClientOption> CreateClientOptions() =>
    [
        new("vscode_copilot", _localizationService.GetString("mcp.client.vscode_copilot")),
        new("claude_desktop", _localizationService.GetString("mcp.client.claude_desktop")),
        new("cursor", _localizationService.GetString("mcp.client.cursor")),
        new("generic_stdio", _localizationService.GetString("mcp.client.generic_stdio"))
    ];

    private void SetStatus(string key, object? argument = null, bool isError = false)
    {
        _statusKey = key;
        _statusArguments = argument is null ? [] : [argument];
        StatusMessage = _localizationService.GetString(key, _statusArguments);
        IsError = isError;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedClientId = SelectedClient?.Id;
        Clients = CreateClientOptions();
        SelectedClient = Clients.FirstOrDefault(client => client.Id == selectedClientId) ?? Clients[0];
        OnPropertyChanged(nameof(Clients));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(ExecutableStatusLabel));
        OnPropertyChanged(nameof(ExecutableStatus));
        OnPropertyChanged(nameof(ClientLabel));
        OnPropertyChanged(nameof(TransportLabel));
        OnPropertyChanged(nameof(TransportValue));
        OnPropertyChanged(nameof(ConfigurationLabel));
        OnPropertyChanged(nameof(CopyConfigurationButtonLabel));

        if (_statusKey is not null)
        {
            StatusMessage = _localizationService.GetString(_statusKey, _statusArguments);
        }
    }
}
