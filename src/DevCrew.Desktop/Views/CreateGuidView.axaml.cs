using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using DevCrew.Desktop.ViewModels;

namespace DevCrew.Desktop.Views;

/// <summary>
/// View for creating and managing GUIDs
/// </summary>
public partial class CreateGuidView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateGuidView"/> class.
    /// </summary>
    public CreateGuidView()
    {
        InitializeComponent();
        var scrollViewer = this.FindControl<ScrollViewer>("GuidsScrollViewer");
        if (scrollViewer is not null)
        {
            scrollViewer.ScrollChanged += OnGuidsScrollChanged;
        }
        Loaded += async (_, _) =>
        {
            if (DataContext is CreateGuidViewModel viewModel)
            {
                await viewModel.LoadSavedGuidsAsync();
            }
        };
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var source = e.Source as Control;
        if (source is null)
            return;

        if (source is TextBox || source.FindLogicalAncestorOfType<TextBox>() is not null)
            return;

        if (TopLevel.GetTopLevel(this)?.FocusManager is { } focusManager)
        {
            focusManager.ClearFocus();
        }
    }

    private async void OnGuidsScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (DataContext is not CreateGuidViewModel viewModel)
            return;

        if (!viewModel.ShowOnlySavedGuids || viewModel.IsLoadingMore || !viewModel.HasMoreSavedGuids)
            return;

        if (sender is not ScrollViewer scrollViewer)
            return;

        var nearBottom = scrollViewer.Offset.Y + scrollViewer.Viewport.Height >= scrollViewer.Extent.Height - 80;
        if (nearBottom)
        {
            await viewModel.LoadMoreSavedGuidsAsync();
        }
    }
}
