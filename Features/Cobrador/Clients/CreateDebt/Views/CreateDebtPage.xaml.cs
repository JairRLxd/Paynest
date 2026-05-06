using Paynest.Features.Cobrador.Clients.CreateDebt.ViewModels;

namespace Paynest.Features.Cobrador.Clients.CreateDebt.Views;

public partial class CreateDebtPage : ContentPage
{
    public CreateDebtPage(CreateDebtViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
