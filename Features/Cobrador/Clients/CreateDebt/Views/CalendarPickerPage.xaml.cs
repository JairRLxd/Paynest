using Paynest.Features.Cobrador.Clients.CreateDebt.ViewModels;

namespace Paynest.Features.Cobrador.Clients.CreateDebt.Views;

public partial class CalendarPickerPage : ContentPage
{
    public CalendarPickerPage()
    {
        InitializeComponent();
        BindingContext = new CalendarPickerViewModel();
    }
}
