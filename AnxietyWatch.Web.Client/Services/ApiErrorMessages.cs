namespace AnxietyWatch.Web.Client.Services;

public static class ApiErrorMessages
{
    public static string For(ApiException exception, string fallback = "No pudimos completar la operación.") =>
        exception.StatusCode switch
        {
            400 => "Revisa los datos ingresados e inténtalo nuevamente.",
            401 => "Tu sesión ha expirado. Inicia sesión nuevamente.",
            403 => "Tu plan actual no permite realizar esta operación.",
            404 => "No encontramos el recurso solicitado.",
            409 => "La operación entra en conflicto con el estado actual.",
            410 => "El recurso solicitado ya no está disponible.",
            429 when exception.RetryAfterSeconds is > 0 =>
                $"Demasiados intentos. Inténtalo nuevamente en {exception.RetryAfterSeconds} segundos.",
            429 => "Demasiados intentos. Espera un momento antes de volver a intentarlo.",
            >= 500 => "El servicio no está disponible en este momento. Inténtalo más tarde.",
            _ => fallback
        };
}
