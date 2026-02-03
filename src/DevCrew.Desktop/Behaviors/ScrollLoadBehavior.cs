using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace DevCrew.Desktop.Behaviors;

public sealed class ScrollLoadBehavior
{
    private ScrollLoadBehavior()
    {
    }
    public static readonly AttachedProperty<ICommand?> LoadMoreCommandProperty =
        AvaloniaProperty.RegisterAttached<ScrollLoadBehavior, ScrollViewer, ICommand?>("LoadMoreCommand");

    public static readonly AttachedProperty<double> ThresholdProperty =
        AvaloniaProperty.RegisterAttached<ScrollLoadBehavior, ScrollViewer, double>("Threshold", 80d);

    public static ICommand? GetLoadMoreCommand(ScrollViewer control) => control.GetValue(LoadMoreCommandProperty);

    public static void SetLoadMoreCommand(ScrollViewer control, ICommand? value) => control.SetValue(LoadMoreCommandProperty, value);

    public static double GetThreshold(ScrollViewer control) => control.GetValue(ThresholdProperty);

    public static void SetThreshold(ScrollViewer control, double value) => control.SetValue(ThresholdProperty, value);

    static ScrollLoadBehavior()
    {
        LoadMoreCommandProperty.Changed.AddClassHandler<ScrollViewer>(OnLoadMoreCommandChanged);
    }

    private static void OnLoadMoreCommandChanged(ScrollViewer scrollViewer, AvaloniaPropertyChangedEventArgs e)
    {
        scrollViewer.ScrollChanged -= OnScrollChanged;

        if (e.NewValue is not null)
        {
            scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    private static void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
            return;

        var command = GetLoadMoreCommand(scrollViewer);
        if (command is null)
            return;

        var threshold = GetThreshold(scrollViewer);
        var nearBottom = scrollViewer.Offset.Y + scrollViewer.Viewport.Height >= scrollViewer.Extent.Height - threshold;
        if (!nearBottom)
            return;

        if (command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}
