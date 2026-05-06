namespace Paynest.Core.Models.Cobrador.Clients;

public sealed record UpdateClientRequest(
    string  Name,
    string? Phone,
    string? Curp,
    string? Rfc,
    string? Address,
    string? PostalCode,
    string? Colonia,
    string? Municipio,
    string? Estado,
    string? Notes);
