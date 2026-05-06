using Paynest.Core.Models.Profile;

namespace Paynest.Core.Interfaces;

public interface IDocumentImageProcessor
{
    Task<PreparedUploadFile> PrepareForUploadAsync(
        FileResult file,
        DocumentType documentType,
        ImageUploadFormat format = ImageUploadFormat.Webp,
        CancellationToken ct = default);
}
