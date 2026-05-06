using System.Text.RegularExpressions;

namespace Paynest.Core.Validation;

public static partial class AppValidators
{
    [GeneratedRegex(@"^\d{10}$")]
    private static partial Regex PhoneRegex();

    // CURP: 4 letters + 6 digits (YYMMDD) + H/M + 2-letter state + 3 consonants + 1 alphanum + 1 digit
    [GeneratedRegex(@"^[A-Z]{4}\d{6}[HM][A-Z]{5}[A-Z0-9]\d$", RegexOptions.IgnoreCase)]
    private static partial Regex CurpRegex();

    // RFC Persona: 4 letters + 6 digits + 3 homoclave = 13 chars
    [GeneratedRegex(@"^[A-Z&]{4}\d{6}[A-Z\d]{3}$", RegexOptions.IgnoreCase)]
    private static partial Regex RfcPersonaRegex();

    // RFC Empresa: 3 letters + 6 digits + 3 homoclave = 12 chars
    [GeneratedRegex(@"^[A-Z&]{3}\d{6}[A-Z\d]{3}$", RegexOptions.IgnoreCase)]
    private static partial Regex RfcEmpresaRegex();

    // CLABE: 18 digits
    [GeneratedRegex(@"^\d{18}$")]
    private static partial Regex ClabeRegex();

    public static bool IsValidPhone(string? value)
        => !string.IsNullOrWhiteSpace(value) && PhoneRegex().IsMatch(value.Trim());

    public static bool IsValidCurp(string? value)
        => !string.IsNullOrWhiteSpace(value) && CurpRegex().IsMatch(value.Trim());

    public static bool IsValidRfc(string? value, bool isEmpresa)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim();
        return isEmpresa ? RfcEmpresaRegex().IsMatch(v) : RfcPersonaRegex().IsMatch(v);
    }

    public static bool IsValidClabe(string? value)
        => !string.IsNullOrWhiteSpace(value) && ClabeRegex().IsMatch(value.Trim());

    public static bool IsValidImageFile(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".jpg" or ".jpeg" or ".png";
    }

    public static bool IsValidUploadSourceImage(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".heic" or ".heif";
    }
}
