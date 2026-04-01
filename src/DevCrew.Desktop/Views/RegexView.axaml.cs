using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using DevCrew.Desktop.Controls;
using DevCrew.Desktop.ViewModels;

namespace DevCrew.Desktop.Views;

public partial class RegexView : UserControl
{
    private Border? _inputBorder;
    private TextBox? _inputTextBox;
    private RegexHighlightOverlay? _highlightOverlay;
    private ScrollViewer? _editorScrollViewer;
    private TextPresenter? _textPresenter;
    private RegexViewModel? _viewModel;

    public RegexView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        AttachControls();
        AttachDragDropHandlers();
        RefreshOverlay();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        _viewModel = DataContext as RegexViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        RefreshOverlay();
    }

    private void AttachControls()
    {
        _inputBorder = this.FindControl<Border>("InputBorder");
        _inputTextBox = this.FindControl<TextBox>("InputTextBox");
        _highlightOverlay = this.FindControl<RegexHighlightOverlay>("HighlightOverlay");

        if (_inputTextBox is null)
        {
            return;
        }

        _textPresenter = _inputTextBox.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
        _editorScrollViewer = _inputTextBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

        _inputTextBox.PropertyChanged -= InputTextBox_PropertyChanged;
        _inputTextBox.PropertyChanged += InputTextBox_PropertyChanged;
        _inputTextBox.PointerMoved -= InputTextBox_PointerMoved;
        _inputTextBox.PointerMoved += InputTextBox_PointerMoved;
        _inputTextBox.PointerExited -= InputTextBox_PointerExited;
        _inputTextBox.PointerExited += InputTextBox_PointerExited;

        if (_editorScrollViewer is not null)
        {
            _editorScrollViewer.ScrollChanged -= EditorScrollViewer_ScrollChanged;
            _editorScrollViewer.ScrollChanged += EditorScrollViewer_ScrollChanged;
        }
    }

    private void AttachDragDropHandlers()
    {
        if (_inputBorder is null)
        {
            return;
        }

        _inputBorder.AddHandler(DragDrop.DropEvent, InputBorder_Drop);
        _inputBorder.AddHandler(DragDrop.DragOverEvent, InputBorder_DragOver);
        _inputBorder.AddHandler(DragDrop.DragLeaveEvent, InputBorder_DragLeave);
    }

    private async void InputBorder_Drop(object? sender, DragEventArgs e)
    {
        try
        {
            if (!e.Data.Contains(DataFormats.Files))
            {
                return;
            }

            var files = e.Data.GetFiles();
            if (files == null || !files.Any() || _viewModel is null)
            {
                return;
            }

            await _viewModel.SetSelectedFileAsync(files.First().Path.LocalPath);
            e.Handled = true;
        }
        finally
        {
            ResetDropStyle();
        }
    }

    private void InputBorder_DragOver(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files) || _inputBorder is null)
        {
            return;
        }

        _inputBorder.BorderBrush = Brushes.LimeGreen;
        _inputBorder.BorderThickness = new Thickness(2);
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void InputBorder_DragLeave(object? sender, DragEventArgs e)
    {
        ResetDropStyle();
        e.Handled = true;
    }

    private void ResetDropStyle()
    {
        if (_inputBorder is null)
        {
            return;
        }

        _inputBorder.BorderBrush = (IBrush?)this.FindResource("BrushBorderSecondary") ?? Brushes.Gray;
        _inputBorder.BorderThickness = new Thickness(1);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RegexViewModel.Matches) or nameof(RegexViewModel.InputText))
        {
            RefreshOverlay();
        }
    }

    private void InputTextBox_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty || e.Property == TextBox.TextProperty)
        {
            RefreshOverlay();
        }
    }

    private void EditorScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        RefreshOverlay();
    }

    private void InputTextBox_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_highlightOverlay is null || _inputTextBox is null)
        {
            return;
        }

        var point = e.GetPosition(_inputTextBox);
        var match = _highlightOverlay.GetMatchAt(point);
        _highlightOverlay.SetHoveredMatch(match);
        ToolTip.SetTip(_inputTextBox, match?.TooltipText);
        ToolTip.SetIsOpen(_inputTextBox, match is not null);
    }

    private void InputTextBox_PointerExited(object? sender, PointerEventArgs e)
    {
        if (_highlightOverlay is null || _inputTextBox is null)
        {
            return;
        }

        _highlightOverlay.SetHoveredMatch(null);
        ToolTip.SetIsOpen(_inputTextBox, false);
    }

    private void RefreshOverlay()
    {
        if (_inputTextBox is null || _highlightOverlay is null || _viewModel is null)
        {
            return;
        }

        _textPresenter ??= _inputTextBox.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
        _editorScrollViewer ??= _inputTextBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

        var fontFamily = _inputTextBox.FontFamily ?? FontFamily.Default;
        var typeface = new Typeface(fontFamily, _inputTextBox.FontStyle, _inputTextBox.FontWeight);

        _highlightOverlay.UpdatePresentation(
            _textPresenter?.TextLayout,
            _inputTextBox.Text ?? string.Empty,
            _viewModel.Matches,
            _inputTextBox.Padding,
            _editorScrollViewer?.Offset ?? default,
            typeface,
            _inputTextBox.FontSize);
    }
}