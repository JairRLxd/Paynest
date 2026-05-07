#nullable enable
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Clients;
using Paynest.Core.Models.Cobrador.Clients.CreateDebt;
using Paynest.Core.Models.Cobrador.Clients.RegisterPayment;
using Paynest.Core.Models.Cobrador.Collections;
using Paynest.Core.Models.Cobrador.Dashboard;
using QRCoder;

namespace Paynest.Features.Cobrador.Mocks;

// MOCK_SWAP_POINT: eliminar este archivo cuando el modulo cobrador use solo backend real.
public sealed class MockCollectorClientService : ICollectorClientService
{
    private static readonly CollectorClientSummaryDto[] Clients =
    [
        new("client_001", "Maria Garcia", "MG", "Atrasado", 4200m, "Vence hoy", null),
        new("client_002", "Carlos Torres", "CT", "Al corriente", 1800m, "15 May", null)
    ];

    public Task<CollectorClientListResponse> GetClientsAsync(CancellationToken ct = default)
        => Task.FromResult(new CollectorClientListResponse(Clients, Clients.Length));

    public Task<CollectorClientDetailResponse> GetClientDetailAsync(string clientId, CancellationToken ct = default)
        => Task.FromResult(new CollectorClientDetailResponse(
            clientId,
            Clients.FirstOrDefault(x => x.ClientId == clientId)?.Name ?? "Cliente demo",
            Clients.FirstOrDefault(x => x.ClientId == clientId)?.Initials ?? "CD",
            "cliente.demo@paynest.local",
            "5512345678",
            "XXXX000000HDFXXX00",
            "XAXX010101000",
            "Av. Reforma 123",
            "06600",
            "Juarez",
            "Cuauhtemoc",
            "CDMX",
            DateTime.Today.AddMonths(-2),
            true,
            true));

    public Task<CollectorClientFinancialSummaryResponse> GetFinancialSummaryAsync(string clientId, CancellationToken ct = default)
        => Task.FromResult(new CollectorClientFinancialSummaryResponse(
            clientId,
            18500m,
            6200m,
            2050m,
            1200m,
            180m,
            2050m,
            3.5m,
            "Atrasado",
            "Tiene una cuota vencida pendiente de regularizar.",
            true,
            DateTime.Today.AddDays(8)));

    public Task UpdateClientAsync(string clientId, UpdateClientRequest request, CancellationToken ct = default)
        => Task.CompletedTask;
}

public sealed class MockCollectorDashboardService : ICollectorDashboardService
{
    public Task<CollectorDashboardResponse> GetDashboardAsync(CancellationToken ct = default)
        => Task.FromResult(new CollectorDashboardResponse(6200m, 12m, true, 18400m, 25000m, 78000m, 8, 9600m, 3));
}

public sealed class MockCollectorCollectionsService : ICollectorCollectionsService
{
    public Task<CollectorCollectionsResponse> GetCollectionsAsync(CancellationToken ct = default)
        => Task.FromResult(new CollectorCollectionsResponse(
            78000m,
            8,
            3,
            9600m,
            [
                new("debt_001", "client_001", "Maria Garcia", "MG", "Branding + Web", 3, DateTime.Today, "Vence hoy", 2050m, 2050m, 180m, 3.5m, 2230m, "Vencido", true, false, true),
                new("debt_002", "client_002", "Carlos Torres", "CT", "Contenido mensual", 2, DateTime.Today.AddDays(5), "Vence 5 dias", 1800m, 1800m, 0m, 0m, 1800m, "Pendiente", false, false, false)
            ]));
}

public sealed class MockCollectorDebtService : ICollectorDebtService
{
    public Task<CollectorDebtPreviewResponse> PreviewAsync(string clientId, CollectorDebtPreviewRequest request, CancellationToken ct = default)
    {
        var total = request.Amount + (request.Amount * ((request.InterestRate ?? 0m) / 100m));
        return Task.FromResult(new CollectorDebtPreviewResponse
        {
            PrincipalAmount = request.Amount,
            NormalInterestRate = request.InterestRate ?? 0m,
            NormalInterestAmount = total - request.Amount,
            MoratoryRate = request.MoratoryRate ?? 0m,
            TotalAmount = total,
            Periodicity = request.Periodicidad.ToString(),
            CalculationMode = request.CalculationMode.ToString(),
            InstallmentsCount = 6,
            ScheduledInstallmentAmount = Math.Round(total / 6m, 2),
            LastInstallmentAmount = Math.Round(total / 6m, 2),
            StartDate = request.StartDate.ToDateTime(TimeOnly.MinValue),
            FirstPaymentDate = request.FirstPaymentDate.ToDateTime(TimeOnly.MinValue),
            DueDate = request.DueDate.ToDateTime(TimeOnly.MinValue),
            InstallmentTitle = "Vista previa mock",
            Footnote = "MOCK_SWAP_POINT"
        });
    }

    public Task<CollectorDebtDetailResponse> CreateAsync(string clientId, CollectorDebtCreateRequest request, CancellationToken ct = default)
        => Task.FromResult(CreateDetail(clientId, $"debt_{DateTime.UtcNow:yyyyMMddHHmmss}", request.Description, request.Amount));

    public Task<CollectorDebtDetailResponse> GetDebtAsync(string clientId, string debtId, CancellationToken ct = default)
        => Task.FromResult(CreateDetail(clientId, debtId, "Deuda demo", 18500m));

    public Task<CollectorDebtOpenSummaryResponse> GetOpenSummaryAsync(string clientId, CancellationToken ct = default)
        => Task.FromResult(new CollectorDebtOpenSummaryResponse
        {
            ClientId = clientId,
            ActiveDebtsCount = 2,
            OpenInstallmentsCount = 4,
            TotalOutstandingAmount = 12300m,
            TotalOverdueAmount = 2050m,
            NextDueDate = DateTime.Today.AddDays(8),
            OpenInstallments =
            [
                new() { DebtId = "debt_001", Description = "Branding + Web", InstallmentNumber = 3, DueDate = DateTime.Today.AddDays(-2), Amount = 2050m, RemainingAmount = 2050m, IsOverdue = true },
                new() { DebtId = "debt_001", Description = "Branding + Web", InstallmentNumber = 4, DueDate = DateTime.Today.AddDays(8), Amount = 2050m, RemainingAmount = 2050m, IsOverdue = false }
            ]
        });

    public Task<IReadOnlyList<PendingCollectorDebtResponse>> GetPendingAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PendingCollectorDebtResponse>>(
        [
            new() { DebtId = "debt_001", ClientId = "client_001", Description = "Branding + Web", InstallmentNumber = 3, DueDate = DateTime.Today.AddDays(-2), Amount = 2050m, RemainingAmount = 2050m, IsOverdue = true }
        ]);

    private static CollectorDebtDetailResponse CreateDetail(string clientId, string debtId, string description, decimal amount)
        => new()
        {
            DebtId = debtId,
            ClientId = clientId,
            CollectorUserId = "collector_demo",
            Description = description,
            PrincipalAmount = amount,
            NormalInterestRate = 0m,
            NormalInterestAmount = 0m,
            MoratoryRate = 3.5m,
            TotalAmount = amount,
            Periodicity = "Mensual",
            CalculationMode = "ByInstallmentAmount",
            InstallmentsCount = 6,
            ScheduledInstallmentAmount = Math.Round(amount / 6m, 2),
            LastInstallmentAmount = Math.Round(amount / 6m, 2),
            StartDate = DateTime.Today,
            FirstPaymentDate = DateTime.Today.AddDays(15),
            DueDate = DateTime.Today.AddMonths(6),
            Status = "Active",
            CreatedAt = DateTime.Now
        };
}

public sealed class MockCollectorPaymentService : ICollectorPaymentService
{
    public Task<PaymentPreviewResponse> PreviewAsync(string clientId, PreviewPaymentRequest request, CancellationToken ct = default)
        => Task.FromResult(new PaymentPreviewResponse(
            clientId,
            12300m,
            2050m,
            2050m,
            1200m,
            180m,
            3.5m,
            request.Amount ?? 2050m,
            true,
            "Pendiente",
            [new PaymentDebtSummaryItemModel { DebtId = "debt_001", Title = "Branding + Web", Subtitle = "Vence hoy", PrincipalAmount = 2050m, MoratoryAmount = 180m, TotalAmount = 2230m, HasMoratoryAmount = true, IsOverdue = true }],
            [new PaymentAllocationPreviewItemModel("debt_001", 3, DateTime.Today, 180m, request.Amount ?? 2050m)]));

    public Task<RegisterPaymentResponse> RegisterAsync(string clientId, RegisterPaymentRequest request, CancellationToken ct = default)
        => Task.FromResult(new RegisterPaymentResponse($"pay_{DateTime.UtcNow:yyyyMMddHHmmss}", clientId, request.Amount, request.Method, request.IsTotalPayment, request.PaymentDateTime ?? DateTime.Now, request.Notes));

    public Task<IReadOnlyList<PaymentHistoryItemModel>> GetHistoryAsync(string clientId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PaymentHistoryItemModel>>(
        [
            new("pay_001", clientId, 2050m, "Cash", false, DateTime.Today.AddDays(-8), "Pago registrado en mock")
        ]);
}

public sealed class MockCollectorInviteService : ICollectorInviteService
{
    private const string CollectorCode = "PAY-X7XTM5";

    public Task<CollectorInviteDto> GetInviteAsync(CancellationToken ct = default)
        => Task.FromResult(new CollectorInviteDto
        {
            CollectorId = "collector_demo",
            Code = CollectorCode,
            CollectorCode = CollectorCode,
            QrPayload = $"paynest://collector/link?code={CollectorCode}",
            CreatedAt = DateTime.Today.AddMonths(-1),
            Status = "active",
            IsLocalFallback = false
        });

    public string GetOrCreateCollectorCode(string collectorId) => CollectorCode;

    public byte[] GenerateQrPng(string collectorCode, int pixelsPerModule = 12)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(collectorCode, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(qrData).GetGraphic(pixelsPerModule);
    }
}
