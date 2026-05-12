using Paynest.Core.Models.Cobrador.Clients;
using Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

namespace Paynest.Features.Cobrador.UseCases;

public sealed record ClientDetailResult(
    CollectorClientDetailResponse           Profile,
    CollectorClientFinancialSummaryResponse Financial,
    IReadOnlyList<PaymentHistoryItemModel>  History);
