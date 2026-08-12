using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AnxietyWatch.Web.Client.Models.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 60 caracteres.")]
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
    [StringLength(254, ErrorMessage = "El correo no puede superar los 254 caracteres.")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(30, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 30 caracteres.")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("planId")]
    public string PlanId { get; set; } = "free";

    [JsonPropertyName("billingCycle")]
    public string BillingCycle { get; set; } = "monthly";

    [JsonPropertyName("paymentMethodToken")]
    public string? PaymentMethodToken { get; set; }
}
