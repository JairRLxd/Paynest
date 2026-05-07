#nullable enable
using Paynest.Features.Client.Api;

namespace Paynest.Features.Client.Mocks;

// MOCK_SWAP_POINT: eliminar esta clase cuando todos los endpoints cliente esten conectados.
public sealed class MockDebtApiClient : IDebtApiClient
{
    private readonly List<DebtGroupDto> _groups =
    [
        new()
        {
            Id = "grp_branding_web",
            Name = "Branding + Web",
            FreelancerName = "Alex Rivera",
            TotalAmount = 18500m,
            PendingAmount = 12300m,
            Frequency = "Mensual"
        },
        new()
        {
            Id = "grp_social_media",
            Name = "Contenido mensual",
            FreelancerName = "Mariana López",
            TotalAmount = 7200m,
            PendingAmount = 2400m,
            Frequency = "Quincenal"
        }
    ];

    private readonly List<InstallmentDto> _installments =
    [
        new() { Id = "ins_001", GroupId = "grp_branding_web", Title = "Cuota 3 de 6", DueDate = DateTime.Today.AddDays(-2), Amount = 2050m, Status = "overdue" },
        new() { Id = "ins_002", GroupId = "grp_branding_web", Title = "Cuota 4 de 6", DueDate = DateTime.Today.AddDays(8), Amount = 2050m, Status = "pending" },
        new() { Id = "ins_003", GroupId = "grp_branding_web", Title = "Cuota 2 de 6", DueDate = DateTime.Today.AddDays(-32), Amount = 2050m, Status = "paid" },
        new() { Id = "ins_004", GroupId = "grp_social_media", Title = "Entrega mayo", DueDate = DateTime.Today.AddDays(4), Amount = 1200m, Status = "pending" },
        new() { Id = "ins_005", GroupId = "grp_social_media", Title = "Entrega abril", DueDate = DateTime.Today.AddDays(-10), Amount = 1200m, Status = "paid" }
    ];

    private readonly List<ReceiptDto> _receipts =
    [
        new() { Id = "rcp_001", InstallmentId = "ins_003", GroupId = "grp_branding_web", Title = "Cuota 2 de 6", Amount = 2050m, PaidAt = DateTime.Today.AddDays(-30).AddHours(12), Folio = "PAY-RCP-001", FileUrl = "https://example.invalid/receipts/rcp_001.pdf" },
        new() { Id = "rcp_002", InstallmentId = "ins_005", GroupId = "grp_social_media", Title = "Entrega abril", Amount = 1200m, PaidAt = DateTime.Today.AddDays(-9).AddHours(18), Folio = "PAY-RCP-002", FileUrl = "https://example.invalid/receipts/rcp_002.pdf" }
    ];

    private WalletDto _wallet = new()
    {
        Id = "wallet_demo",
        Balance = 8450m,
        Currency = "MXN",
        Status = "active"
    };

    private readonly List<WalletMovementDto> _movements =
    [
        new() { Id = "mov_001", Type = "deposit", Amount = 5000m, Currency = "MXN", Description = "Abono para pagos de mayo", Reference = "DEP-MAYO", CreatedAt = DateTime.Now.AddHours(-3) },
        new() { Id = "mov_002", Type = "installment_payment", Amount = -1200m, Currency = "MXN", Description = "Pago contenido mensual", Reference = "PAY-RCP-002", RelatedInstallmentId = "ins_005", RelatedPaymentId = "pay_002", CreatedAt = DateTime.Now.AddDays(-9) }
    ];

    public Task<IReadOnlyList<DebtGroupDto>> GetDebtGroupsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DebtGroupDto>>(_groups);

    public Task<IReadOnlyList<InstallmentDto>> GetInstallmentsByGroupAsync(string groupId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InstallmentDto>>(_installments.Where(x => x.GroupId == groupId).ToList());

    public Task<PayInstallmentResponseDto> MarkInstallmentAsPaidAsync(string installmentId, CancellationToken cancellationToken = default)
    {
        var installment = _installments.FirstOrDefault(x => x.Id == installmentId);
        if (installment is null)
            return Task.FromResult(new PayInstallmentResponseDto());

        var payment = new PaymentDto
        {
            Id = $"pay_{DateTime.UtcNow:yyyyMMddHHmmss}",
            InstallmentId = installmentId,
            Amount = installment.Amount,
            Currency = "MXN",
            Method = "wallet",
            Status = "confirmed",
            Reference = $"MOCK-{installmentId}",
            PaidAt = DateTime.Now
        };
        var movement = new WalletMovementDto
        {
            Id = $"mov_{DateTime.UtcNow:yyyyMMddHHmmss}",
            Type = "installment_payment",
            Amount = -installment.Amount,
            Currency = "MXN",
            Description = $"Pago {installment.Title}",
            Reference = payment.Reference,
            RelatedInstallmentId = installmentId,
            RelatedPaymentId = payment.Id,
            CreatedAt = DateTime.Now
        };
        var receipt = new ReceiptDto
        {
            Id = $"rcp_{DateTime.UtcNow:yyyyMMddHHmmss}",
            InstallmentId = installmentId,
            GroupId = installment.GroupId,
            Title = installment.Title,
            Amount = installment.Amount,
            PaidAt = DateTime.Now,
            Folio = payment.Reference,
            FileUrl = null
        };

        _wallet = new WalletDto
        {
            Id = _wallet.Id,
            Balance = Math.Max(0, _wallet.Balance - installment.Amount),
            Currency = _wallet.Currency,
            Status = _wallet.Status
        };
        _movements.Insert(0, movement);
        _receipts.Insert(0, receipt);

        return Task.FromResult(new PayInstallmentResponseDto
        {
            Success = true,
            Wallet = _wallet,
            Payment = payment,
            Receipt = receipt,
            Movement = movement
        });
    }

    public Task<IReadOnlyList<ReceiptDto>> GetReceiptsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ReceiptDto>>(_receipts);

    public Task<ReceiptDto?> GetReceiptAsync(string receiptId, CancellationToken cancellationToken = default)
        => Task.FromResult(_receipts.FirstOrDefault(x => x.Id == receiptId));

    public Task<string?> GetReceiptDownloadUrlAsync(string receiptId, CancellationToken cancellationToken = default)
        => Task.FromResult(_receipts.FirstOrDefault(x => x.Id == receiptId)?.FileUrl);

    public Task<NotificationPreferencesDto> GetNotificationPreferencesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new NotificationPreferencesDto
        {
            NotifyThreeDaysBefore = true,
            NotifySameDay = true,
            ReminderTime = "09:00",
            Timezone = "America/Mexico_City",
            Channels = new NotificationChannelsDto { Push = true, Email = false }
        });

    public Task<NotificationPreferencesDto> UpdateNotificationPreferencesAsync(NotificationPreferencesDto request, CancellationToken cancellationToken = default)
        => Task.FromResult(request);

    public Task<WalletDto> GetWalletAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_wallet);

    public Task<IReadOnlyList<WalletMovementDto>> GetWalletMovementsAsync(int limit = 20, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<WalletMovementDto>>(_movements.Take(Math.Clamp(limit, 1, 100)).ToList());

    public Task<WalletDepositResponseDto> DepositWalletAsync(WalletDepositRequestDto request, CancellationToken cancellationToken = default)
    {
        _wallet = new WalletDto
        {
            Id = _wallet.Id,
            Balance = _wallet.Balance + request.Amount,
            Currency = _wallet.Currency,
            Status = _wallet.Status
        };
        var movement = new WalletMovementDto
        {
            Id = $"mov_{DateTime.UtcNow:yyyyMMddHHmmss}",
            Type = "deposit",
            Amount = request.Amount,
            Currency = "MXN",
            Description = string.IsNullOrWhiteSpace(request.Description) ? "Abono de saldo" : request.Description,
            Reference = "MOCK-DEPOSIT",
            CreatedAt = DateTime.Now
        };
        _movements.Insert(0, movement);
        return Task.FromResult(new WalletDepositResponseDto { Wallet = _wallet, Movement = movement });
    }

    public Task<LinkCollectorResponseDto> LinkCollectorAsync(LinkCollectorRequestDto request, CancellationToken cancellationToken = default)
        => Task.FromResult(new LinkCollectorResponseDto
        {
            CollectorId = "collector_demo",
            CollectorName = "Alex Rivera",
            RelationshipId = "rel_demo",
            LinkedAt = DateTime.Now
        });
}
