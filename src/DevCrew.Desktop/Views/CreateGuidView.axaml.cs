using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using DevCrew.Desktop.ViewModels;

namespace DevCrew.Desktop.Views;

public partial class CreateGuidView : UserControl
{
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
