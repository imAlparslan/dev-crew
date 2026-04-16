using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit;
using DevCrew.Desktop.Behaviors;
using DevCrew.Desktop.Controls;
using DevCrew.Desktop.ViewModels;

namespace DevCrew.Desktop.Views;

public partial class RegexView : UserControl
{
    private Border? _inputBorder;
    private TextEditor? _inputEditor;
    private readonly RegexMatchColorizingTransformer _matchColorizer = new();
    private RegexViewModel? _viewModel;
    private bool _syncingEditorText;

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
        RefreshHighlighting();
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

        RefreshHighlighting();
    }

    private void AttachControls()
    {
        _inputBorder = this.FindControl<Border>("InputBorder");
        _inputEditor = this.FindControl<TextEditor>("InputEditor");

        if (_inputEditor is null)
        {
            return;
        }

        if (!_inputEditor.TextArea.TextView.BackgroundRenderers.Contains(_matchColorizer))
        {
            _inputEditor.TextArea.TextView.BackgroundRenderers.Add(_matchColorizer);
        }

        _inputEditor.TextChanged -= InputEditor_TextChanged;
        _inputEditor.TextChanged += InputEditor_TextChanged;

        HoverHighlightBehavior.SetHighlightSource(_inputEditor, _matchColorizer);
    }

    private void InputEditor_TextChanged(object? sender, EventArgs e)
    {
        if (_syncingEditorText || _inputEditor is null || _viewModel is null)
        {
            return;
        }

        // During active typing, match refresh is async; clear stale hover/highlight immediately
        // so small-screen wraps do not keep tooltips at outdated offsets.
        _matchColorizer.SetMatches([]);
        _inputEditor.TextArea.TextView.InvalidateLayer(_matchColorizer.Layer);
        ToolTip.SetTip(_inputEditor, null);
        ToolTip.SetIsOpen(_inputEditor, false);

        var editorText = _inputEditor.Text ?? string.Empty;
        if (!string.Equals(_viewModel.InputText, editorText, StringComparison.Ordinal))
        {
            _viewModel.InputText = editorText;
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
            RefreshHighlighting();
        }
    }

    private void RefreshHighlighting()
    {
        if (_inputEditor is null || _viewModel is null)
        {
            return;
        }

        var viewModelText = _viewModel.InputText ?? string.Empty;
        if (!string.Equals(_inputEditor.Text, viewModelText, StringComparison.Ordinal))
        {
            _syncingEditorText = true;
            try
            {
                _inputEditor.Text = viewModelText;
            }
            finally
            {
                _syncingEditorText = false;
            }
        }

        _matchColorizer.SetMatches(_viewModel.Matches);
        _inputEditor.TextArea.TextView.InvalidateLayer(_matchColorizer.Layer);
    }
}
