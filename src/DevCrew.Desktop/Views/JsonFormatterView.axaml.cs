using Avalonia.Controls;
using Avalonia.Input;
using DevCrew.Desktop.ViewModels;

namespace DevCrew.Desktop.Views;

public partial class JsonFormatterView : UserControl
{
    private Border? _inputBorder;

    public JsonFormatterView()
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
            _inputBorder.AddHandler(DragDrop.DragOverEvent, InputBorder_DragEnter);
            _inputBorder.AddHandler(DragDrop.DragLeaveEvent, InputBorder_DragLeave);
        }
    }

    private async void InputBorder_Drop(object? sender, DragEventArgs e)
    {
        try
        {
            if (e.Data.Contains(DataFormats.Files))
            {
                var files = e.Data.GetFiles();
                if (files == null || !files.Any())
                    return;

                var filePath = files.First().Path.LocalPath;

                if (!System.IO.File.Exists(filePath))
                {
                    UpdateValidationMessage("Dosya bulunamadı", isError: true);
                    return;
                }

                var fileExtension = System.IO.Path.GetExtension(filePath);
                var fileContent = await System.IO.File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8);

                if (this.DataContext is JsonFormatterViewModel viewModel)
                {
                    viewModel.InputJson = fileContent;
                    viewModel.SourceFileExtension = fileExtension;
                    UpdateValidationMessage($"Dosya yüklendi: {System.IO.Path.GetFileName(filePath)}", isError: false);
                }
            }

            e.Handled = true;
        }
        catch (UnauthorizedAccessException)
        {
            UpdateValidationMessage("Dosyaya erişim reddedildi", isError: true);
        }
        catch (System.IO.IOException ex)
        {
            UpdateValidationMessage($"Dosya okuma hatası: {ex.Message}", isError: true);
        }
        catch (Exception ex)
        {
            UpdateValidationMessage($"Hata: {ex.Message}", isError: true);
        }
    }

    private void InputBorder_DragEnter(object? sender, DragEventArgs e)
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

    private void UpdateValidationMessage(string message, bool isError)
    {
        if (this.DataContext is JsonFormatterViewModel viewModel)
        {
            viewModel.ValidationMessage = message;
            viewModel.IsError = isError;
            viewModel.IsValid = !isError;
        }
    }
}
