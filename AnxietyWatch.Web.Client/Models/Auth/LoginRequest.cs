using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AnxietyWatch.Web.Client.Models.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(254, ErrorMessage = "El correo no puede superar los 254 caracteres.")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(64, ErrorMessage = "La contraseña no puede superar los 64 caracteres.")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
