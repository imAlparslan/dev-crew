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
    /// Threshold distance from bottom to trigger load more (pixels)
    /// </summary>
    private const double ScrollThresholdFromBottom = 80;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateGuidView"/> class.
    /// </summary>
    public CreateGuidView()
    {
        InitializeComponent();
        Loaded += OnViewLoaded;
    }

    /// <summary>
    /// Handles view loaded event to initialize scroll listener and load initial data
    /// </summary>
    protected virtual async void OnViewLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Loaded -= OnViewLoaded;

        var scrollViewer = this.FindControl<ScrollViewer>("GuidsScrollViewer");
        if (scrollViewer is not null)
        {
            scrollViewer.ScrollChanged += OnGuidsScrollChanged;
        }

        if (DataContext is CreateGuidViewModel viewModel)
        {
            await viewModel.LoadSavedGuidsAsync();
        }
    }

    /// <summary>
    /// Handles pointer pressed to clear focus from input fields
    /// </summary>
    protected virtual void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
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

    /// <summary>
    /// Handles scroll changed to load more items when near bottom
    /// </summary>
    protected virtual async void OnGuidsScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (DataContext is not CreateGuidViewModel viewModel)
            return;

        if (!viewModel.ShowOnlySavedGuids || viewModel.IsLoadingMore || !viewModel.HasMoreSavedGuids)
            return;

        if (sender is not ScrollViewer scrollViewer)
            return;

        var nearBottom = scrollViewer.Offset.Y + scrollViewer.Viewport.Height >= scrollViewer.Extent.Height - ScrollThresholdFromBottom;
        if (nearBottom)
        {
            await viewModel.LoadMoreSavedGuidsAsync();
        }
    }
}
