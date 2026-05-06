namespace Paynest.Core.Models.Profile;

public record UserProfileRequest(
    string VisibleName,
    string Phone,
    string PostalCode,
    string Colonia,
    string Municipio,
    string Estado,
    string Address,
    string Curp,
    string Rfc,
    OperationType OperationType,
    UserRole Role = UserRole.AdminCollector,
    string? BusinessName = null,
    string? BusinessType = null,
    string? BusinessLogoPath = null
);
