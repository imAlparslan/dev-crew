using Avalonia.Controls;
using Avalonia.Input;
using DevCrew.Desktop.ViewModels;

namespace DevCrew.Desktop.Views;

public partial class Base64EncoderView : UserControl
{
    private Border? _inputBorder;

    public Base64EncoderView()
    {
        InitializeComponent();
        Loaded += (s, e) => AttachDragDropHandlers();
    }

    private void AttachDragDropHandlers()
    {
        _inputBorder = this.FindControl<Border>("InputBorder");
        if (_inputBorder != null)
        {
            _inputBorder.AddHandler(DragDrop.DropEvent, InputBorder_Drop);
            _inputBorder.AddHandler(DragDrop.DragOverEvent, InputBorder_DragOver);
            _inputBorder.AddHandler(DragDrop.DragLeaveEvent, InputBorder_DragLeave);
        }
    }

    private void InputBorder_DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            if (_inputBorder != null)
            {
                _inputBorder.BorderBrush = Avalonia.Media.Brushes.LimeGreen;
                _inputBorder.BorderThickness = new Avalonia.Thickness(2);
            }

            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void InputBorder_DragLeave(object? sender, DragEventArgs e)
    {
        if (_inputBorder != null)
        {
            _inputBorder.BorderBrush = (Avalonia.Media.IBrush?)this.FindResource("BrushBorderSecondary") ?? Avalonia.Media.Brushes.Gray;
            _inputBorder.BorderThickness = new Avalonia.Thickness(1);
        }

        e.Handled = true;
    }

    private void InputBorder_Drop(object? sender, DragEventArgs e)
    {
        try
        {
            if (!e.Data.Contains(DataFormats.Files))
            {
                return;
            }

            var files = e.Data.GetFiles();
            if (files == null || !files.Any())
            {
                return;
            }

            var filePath = files.First().Path.LocalPath;
            if (this.DataContext is Base64EncoderViewModel viewModel)
            {
                viewModel.SetSelectedFile(filePath);
            }

            e.Handled = true;
        }
        finally
        {
            if (_inputBorder != null)
            {
                _inputBorder.BorderBrush = (Avalonia.Media.IBrush?)this.FindResource("BrushBorderSecondary") ?? Avalonia.Media.Brushes.Gray;
                _inputBorder.BorderThickness = new Avalonia.Thickness(1);
            }
        }
    }
}
