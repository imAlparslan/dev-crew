using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DevCrew.Desktop.ViewModels;

namespace DevCrew.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
