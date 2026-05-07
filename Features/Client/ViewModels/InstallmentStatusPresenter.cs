using Paynest.Models;

namespace Paynest.Features.Client.ViewModels;

public static class InstallmentStatusPresenter
{
    public static (string Text, string Bg, string Fg, string Icon) Present(InstallmentStatus status)
    {
        return status switch
        {
            InstallmentStatus.Paid => ("Pagado", "#EBF5EF", "#2A6349", "✓"),
            InstallmentStatus.Overdue => ("Vencido", "#FEE2E2", "#991B1B", "!"),
            _ => ("Pendiente", "#FEF3C7", "#92400E", "•")
        };
    }
}
