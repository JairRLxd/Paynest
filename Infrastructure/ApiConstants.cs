using System.Reflection;

namespace Paynest.Infrastructure;

public static class ApiConstants
{
    public static string BaseUrl =>
        typeof(ApiConstants).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(x => x.Key == "PaynestBaseUrl")
            ?.Value
        ?? "https://example.invalid";
}
