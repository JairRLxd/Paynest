namespace Paynest.Core.Interfaces;

public interface ICollectorInviteService
{
    Task<CollectorInviteDto> GetInviteAsync(CancellationToken ct = default);
    string GetOrCreateCollectorCode(string collectorId);
    byte[] GenerateQrPng(string collectorCode, int pixelsPerModule = 12);
}

public sealed class CollectorInviteDto
{
    public string CollectorId { get; init; } = string.Empty;
    public string CollectorCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = "active";
    public bool IsLocalFallback { get; init; }
}
