namespace Paynest.Core.Models.Cobrador.Clients;

public sealed record CollectorClientDetailResponse(
    string   ClientId,
    string   VisibleName,
    string   Initials,
    string?  Email,
    string?  Phone,
    string?  Curp,
    string?  Rfc,
    string?  Address,
    string?  PostalCode,
    string?  Colonia,
    string?  Municipio,
    string?  Estado,
    DateTime DateJoined,
    bool     PersonalInfoCompleted,
    bool     DocumentsCompleted);
