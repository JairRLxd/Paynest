namespace Paynest.Core.Models.Profile;

public record PaymentConfigRequest(
    bool EfectivoEnabled,
    bool TransferenciaEnabled,
    string? BankName,
    string? AccountHolder,
    string? Clabe,
    bool TerminalEnabled,
    string? TerminalProvider,
    string? TerminalReference
);
