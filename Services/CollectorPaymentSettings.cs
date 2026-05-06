namespace Paynest.Services;

public class CollectorPaymentSettings
{
    public bool CashEnabled     { get; private set; } = true;
    public bool TransferEnabled { get; private set; } = true;
    public bool CardEnabled     { get; private set; } = true;

    public void Update(bool cash, bool transfer, bool card)
    {
        CashEnabled     = cash;
        TransferEnabled = transfer;
        CardEnabled     = card;
    }
}
