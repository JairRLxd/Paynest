namespace Paynest.Features.Cobrador.Models;

public sealed record ClientProfileSnapshot(
    string  ClientId,
    string  Name,
    string  Initials,
    Color   AvatarColor,
    string? Phone,
    string? Curp,
    string? Rfc,
    string? Address,
    string? PostalCode,
    string? Colonia,
    string? Municipio,
    string? Estado,
    string? Notes);
