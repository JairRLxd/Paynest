using System.Text.RegularExpressions;

namespace Paynest.Core.Validation;

public static partial class InputSanitizer
{
    [GeneratedRegex(@" {2,}")]
    private static partial Regex MultipleSpaces();

    // Free-text: strips control chars (0x00–0x1F, DEL), trims, collapses spaces.
    // Use for: names, addresses, descriptions, notes, business fields.
    public static string Text(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var clean = new string(value.Where(c => c >= ' ' && c != '\x7F').ToArray());
        return MultipleSpaces().Replace(clean.Trim(), " ");
    }

    // Same as Text but returns null when blank — for optional fields.
    public static string? NullableText(string? value)
    {
        var s = Text(value);
        return s.Length == 0 ? null : s;
    }

    // Structured identifiers: keeps only letters, digits and extra allowed chars, uppercased.
    // Use for: CURP, RFC, postal code, CLABE.
    public static string Identifier(string? value, string extra = "")
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return new string(value.Where(c => char.IsLetterOrDigit(c) || extra.Contains(c)).ToArray())
                         .Trim().ToUpperInvariant();
    }

    // Same as Identifier but returns null when blank.
    public static string? NullableIdentifier(string? value, string extra = "")
    {
        var s = Identifier(value, extra);
        return s.Length == 0 ? null : s;
    }

    // Digits only — phone, CLABE, postal code.
    public static string Digits(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return new string(value.Where(char.IsDigit).ToArray());
    }

    // Email: Text + lowercase.
    public static string Email(string? value) => Text(value).ToLowerInvariant();
}
