using System.Collections.ObjectModel;
using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Dashboard view
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    public DashboardViewModel(IErrorHandler errorHandler)
        : base(errorHandler)
    {
        MenuItems = new ObservableCollection<MenuItemViewModel>();
    }

    public ObservableCollection<MenuItemViewModel> MenuItems { get; }
}

