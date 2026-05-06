using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Paynest.Core.Models.PostalCode;

namespace Paynest.Infrastructure.Http;

public class PostalCodeClient(HttpClient http)
{
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public async Task<PostalCodeResponse?> LookupAsync(string cp, string accessToken, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/postal-code/{cp}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var res = await http.SendAsync(req, ct);

        if (res.StatusCode == HttpStatusCode.NotFound)
            return null;

        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<PostalCodeResponse>(json, _opts);
    }
}
