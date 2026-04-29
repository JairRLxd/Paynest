using Paynest.Features.Auth.Register;

namespace Paynest;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Rutas usadas desde ViewModels con GoToAsync
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
    }
}
