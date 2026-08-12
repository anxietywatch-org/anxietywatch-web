using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AnxietyWatch.Web.Client.Models.Auth;

public sealed class ForgotPasswordRequest
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Introduce un correo electrónico válido.")]
    [StringLength(254, ErrorMessage = "El correo no puede superar los 254 caracteres.")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required(ErrorMessage = "El token de recuperación es obligatorio.")]
    [StringLength(2048, ErrorMessage = "El token de recuperación es demasiado largo.")]
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [StringLength(30, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 30 caracteres.")]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class AuthMessageResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public sealed class EmailVerificationStatusResponse
{
    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; }
}
