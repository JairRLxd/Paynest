#nullable enable
using System.Reflection;
using Microsoft.Maui.Devices;

namespace Paynest.Infrastructure;

public static class ApiConstants
{
    private const string DefaultFallbackBaseUrl = "https://example.invalid";
    private const string AndroidEmulatorMetadataKey = "PaynestAndroidEmulatorBaseUrl";
    private const string IosSimulatorMetadataKey = "PaynestiOSSimulatorBaseUrl";
    private const string PhysicalDeviceMetadataKey = "PaynestPhysicalDeviceBaseUrl";
    private const string FallbackMetadataKey = "PaynestBaseUrl";

    public static string BaseUrl => ResolveBaseUrl();

    public static Uri BaseUri => new(BaseUrl, UriKind.Absolute);

    public static string ResolveBaseUrl()
    {
#if DEBUG
        if (DeviceInfo.Current.DeviceType == DeviceType.Virtual)
        {
            if (DeviceInfo.Current.Platform == DevicePlatform.Android)
                return GetConfiguredUrl(AndroidEmulatorMetadataKey);

            if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
                return GetConfiguredUrl(IosSimulatorMetadataKey);

            return FallbackBaseUrl;
        }

        if (DeviceInfo.Current.DeviceType == DeviceType.Physical)
            return GetConfiguredUrl(PhysicalDeviceMetadataKey);
#endif

        return FallbackBaseUrl;
    }

    private static string FallbackBaseUrl =>
        ReadMetadata(FallbackMetadataKey)
        ?? ReadMetadata(PhysicalDeviceMetadataKey)
        ?? DefaultFallbackBaseUrl;

    private static string GetConfiguredUrl(string key)
        => ReadMetadata(key) ?? FallbackBaseUrl;

    private static string? ReadMetadata(string key)
    {
        var value = typeof(ApiConstants).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == key)
            ?.Value;

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
