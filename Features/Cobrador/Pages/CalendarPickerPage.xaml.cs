using Paynest.Features.Cobrador.ViewModels;

namespace Paynest.Features.Cobrador.Pages;

public partial class CalendarPickerPage : ContentPage
{
    public CalendarPickerPage()
    {
        InitializeComponent();
        BindingContext = new CalendarPickerViewModel();
    }
}
