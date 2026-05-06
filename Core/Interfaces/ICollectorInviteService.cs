namespace Paynest.Core.Interfaces;

public interface ICollectorInviteService
{
    string GetOrCreateCollectorCode(string collectorId);
    byte[] GenerateQrPng(string collectorCode, int pixelsPerModule = 12);
}
