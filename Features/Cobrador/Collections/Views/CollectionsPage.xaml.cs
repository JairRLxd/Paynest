using Paynest.Features.Cobrador.Collections.ViewModels;

namespace Paynest.Features.Cobrador.Collections.Views;

public partial class CollectionsPage : ContentPage
{
    public CollectionsPage(CollectionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
