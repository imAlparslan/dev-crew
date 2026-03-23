using Avalonia.Controls;
using Avalonia.Input;
using DevCrew.Desktop.ViewModels;

namespace DevCrew.Desktop.Views;

public partial class JsonDiffView : UserControl
{
    private Border? _leftInputBorder;
    private Border? _rightInputBorder;

    public JsonDiffView()
    {
        InitializeComponent();
        Loaded += (_, _) => AttachDragDropHandlers();
    }

    private void AttachDragDropHandlers()
    {
        _leftInputBorder = this.FindControl<Border>("LeftInputBorder");
        _rightInputBorder = this.FindControl<Border>("RightInputBorder");

        if (_leftInputBorder != null)
        {
            _leftInputBorder.AddHandler(DragDrop.DropEvent, LeftInputBorder_Drop);
            _leftInputBorder.AddHandler(DragDrop.DragOverEvent, LeftInputBorder_DragOver);
            _leftInputBorder.AddHandler(DragDrop.DragLeaveEvent, LeftInputBorder_DragLeave);
        }

        if (_rightInputBorder != null)
        {
            _rightInputBorder.AddHandler(DragDrop.DropEvent, RightInputBorder_Drop);
            _rightInputBorder.AddHandler(DragDrop.DragOverEvent, RightInputBorder_DragOver);
            _rightInputBorder.AddHandler(DragDrop.DragLeaveEvent, RightInputBorder_DragLeave);
        }
    }

    private async void LeftInputBorder_Drop(object? sender, DragEventArgs e)
    {
        await HandleFileDropAsync(e, isLeft: true);
    }

    private async void RightInputBorder_Drop(object? sender, DragEventArgs e)
    {
        await HandleFileDropAsync(e, isLeft: false);
    }

    private void LeftInputBorder_DragOver(object? sender, DragEventArgs e)
    {
        UpdateDragState(_leftInputBorder, e, isOver: true);
    }

    private void RightInputBorder_DragOver(object? sender, DragEventArgs e)
    {
        UpdateDragState(_rightInputBorder, e, isOver: true);
    }

    private void LeftInputBorder_DragLeave(object? sender, DragEventArgs e)
    {
        UpdateDragState(_leftInputBorder, e, isOver: false);
    }

    private void RightInputBorder_DragLeave(object? sender, DragEventArgs e)
    {
        UpdateDragState(_rightInputBorder, e, isOver: false);
    }

    private static void UpdateDragState(Border? border, DragEventArgs e, bool isOver)
    {
        if (border == null)
        {
            return;
        }

        if (isOver && e.Data.Contains(DataFormats.Files))
        {
            border.BorderBrush = Avalonia.Media.Brushes.LimeGreen;
            border.BorderThickness = new Avalonia.Thickness(2);
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;
            return;
        }

        border.BorderBrush = Avalonia.Media.Brushes.Gray;
        border.BorderThickness = new Avalonia.Thickness(1);
        e.Handled = true;
    }

    private async Task HandleFileDropAsync(DragEventArgs e, bool isLeft)
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
            if (!File.Exists(filePath))
            {
                UpdateValidationMessage("Dosya bulunamadı", isError: true);
                return;
            }

            var fileContent = await File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8);
            var extension = Path.GetExtension(filePath);
            var fileName = Path.GetFileName(filePath);

            if (DataContext is JsonDiffViewModel viewModel)
            {
                if (isLeft)
                {
                    viewModel.SetLeftJsonFromDroppedFile(fileContent, extension, fileName);
                }
                else
                {
                    viewModel.SetRightJsonFromDroppedFile(fileContent, extension, fileName);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            UpdateValidationMessage("Dosyaya erişim reddedildi", isError: true);
        }
        catch (IOException ex)
        {
            UpdateValidationMessage($"Dosya okuma hatası: {ex.Message}", isError: true);
        }
        catch (Exception ex)
        {
            UpdateValidationMessage($"Hata: {ex.Message}", isError: true);
        }
        finally
        {
            UpdateDragState(_leftInputBorder, e, isOver: false);
            UpdateDragState(_rightInputBorder, e, isOver: false);
        }
    }

    private void UpdateValidationMessage(string message, bool isError)
    {
        if (DataContext is JsonDiffViewModel viewModel)
        {
            viewModel.ValidationMessage = message;
            viewModel.IsError = isError;
            viewModel.IsValid = !isError;
        }
    }
}
