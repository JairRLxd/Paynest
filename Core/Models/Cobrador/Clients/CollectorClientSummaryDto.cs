namespace Paynest.Core.Models.Cobrador.Clients;

public sealed record CollectorClientSummaryDto(
    string  ClientId,
    string  Name,
    string  Initials,
    string  Status,
    decimal OutstandingAmount,
    string  NextDueDateDisplay,
    string? PhotoUrl);
