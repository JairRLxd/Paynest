using Paynest.Core.Models.Profile;

namespace Paynest.Features.Onboarding;

// Singleton que acumula los datos de los 3 pasos antes de enviarlos a la API
public class OnboardingSession
{
    public UserProfileRequest? PersonalInfo { get; set; }
    // Los documentos se suben por separado en IdentityVerificationViewModel
}
