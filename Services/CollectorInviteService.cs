using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Paynest.Core.Interfaces;
using QRCoder;

namespace Paynest.Services;

public class CollectorInviteService(HttpClient http, AuthStateService authState) : ICollectorInviteService
{
    private const string CodePrefix = "PAY-";
    private const string InvitePath = "/api/v1/collector/invite";

    public Task<CollectorInviteDto> GetInviteAsync(CancellationToken ct = default)
    {
        return authState.CallProtectedAsync(async token =>
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, InvitePath);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await http.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    var invite = await response.Content.ReadFromJsonAsync<CollectorInviteDto>(cancellationToken: ct);
                    if (!string.IsNullOrWhiteSpace(invite?.CollectorCode))
                    {
                        return invite;
                    }
                }
            }
            catch
            {
                // Mientras backend expone el contrato oficial, mantenemos el flujo usable.
            }

            var collectorId = authState.CurrentUser?.Id ?? "anonymous_collector";
            return new CollectorInviteDto
            {
                CollectorId = collectorId,
                CollectorCode = GetOrCreateCollectorCode(collectorId),
                CreatedAt = DateTime.UtcNow,
                Status = "local",
                IsLocalFallback = true
            };
        }, ct);
    }

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
