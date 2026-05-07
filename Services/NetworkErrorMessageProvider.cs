using System.Net;

namespace Paynest.Services;

public static class NetworkErrorMessageProvider
{
    public static string From(Exception ex)
    {
        if (ex is TaskCanceledException)
        {
            return "Tiempo de espera agotado. Intenta nuevamente.";
        }

        if (ex is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout)
            {
                return "El servicio no está disponible por ahora. Intenta en unos minutos.";
            }

            return "Sin conexión con el servidor. Verifica la URL del backend y tu red.";
        }

        return "Sin conexión. Verifica tu internet e intenta de nuevo.";
    }
}
