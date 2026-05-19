using Paynest.Core.Models.Profile;

namespace Paynest.Core.Interfaces;

public interface IProfileService
{
    Task<UserProfileResponse>   GetPersonalInfoAsync(CancellationToken ct = default);
    Task SavePersonalInfoAsync(UserProfileRequest request, CancellationToken ct = default);
    Task UpdatePersonalInfoAsync(UserProfileRequest request, CancellationToken ct = default);
    Task<DocumentsStatusResponse> GetDocumentsStatusAsync(CancellationToken ct = default);
    Task UploadDocumentAsync(DocumentType type, PreparedUploadFile file, CancellationToken ct = default);
    Task<PaymentConfigResponse> GetPaymentConfigAsync(CancellationToken ct = default);
    Task SavePaymentConfigAsync(PaymentConfigRequest request, CancellationToken ct = default);
    Task UpdatePaymentConfigAsync(PaymentConfigRequest request, CancellationToken ct = default);
}
