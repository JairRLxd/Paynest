namespace Paynest.Core.Models.Profile;

public sealed record PreparedUploadFile(
    string FileName,
    string ContentType,
    byte[] Content);

public enum ImageUploadFormat
{
    Webp,
    Jpeg
}
