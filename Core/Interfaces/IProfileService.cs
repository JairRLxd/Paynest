using Paynest.Core.Models.Profile;

namespace Paynest.Core.Interfaces;

public interface IProfileService
{
    Task SavePersonalInfoAsync(UserProfileRequest request, CancellationToken ct = default);
    Task UploadDocumentAsync(DocumentType type, PreparedUploadFile file, CancellationToken ct = default);
    Task SavePaymentConfigAsync(PaymentConfigRequest request, CancellationToken ct = default);
}
