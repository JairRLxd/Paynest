using System.Security.Cryptography;
using System.Text;
using Paynest.Core.Interfaces;
using QRCoder;

namespace Paynest.Services;

public class CollectorInviteService : ICollectorInviteService
{
    private const string CodePrefix = "PAY-";

    public string GetOrCreateCollectorCode(string collectorId)
    {
        if (string.IsNullOrWhiteSpace(collectorId))
            throw new InvalidOperationException("No se encontró la identidad del cobrador.");

        var preferenceKey = $"collector_code_{collectorId}";
        var existingCode = Preferences.Default.Get(preferenceKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existingCode))
            return existingCode;

        var generatedCode = $"{CodePrefix}{CreateStableSuffix(collectorId)}";
        Preferences.Default.Set(preferenceKey, generatedCode);
        return generatedCode;
    }

    public byte[] GenerateQrPng(string collectorCode, int pixelsPerModule = 12)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(collectorCode, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(qrData);
        return pngQrCode.GetGraphic(pixelsPerModule);
    }

    private static string CreateStableSuffix(string collectorId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(collectorId.Trim()));
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        var builder = new StringBuilder(capacity: 6);
        var value = BitConverter.ToUInt32(bytes, 0);

        for (var i = 0; i < 6; i++)
        {
            builder.Append(alphabet[(int)(value % alphabet.Length)]);
            value /= (uint)alphabet.Length;
        }

        return builder.ToString();
    }
}
