namespace Paynest.Infrastructure.Exceptions;

public class ApiException(string message, int? statusCode = null) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
}
