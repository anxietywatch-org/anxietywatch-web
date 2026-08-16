using System.Text.Json.Serialization;

namespace AnxietyWatch.Web.Client.Models.Api;

public sealed class SimulatePaymentRequest
{
    [JsonPropertyName("planId")]
    public string PlanId { get; init; } = string.Empty;

    [JsonPropertyName("billingCycle")]
    public string BillingCycle { get; init; } = string.Empty;
}

public sealed class SimulatePaymentResponse
{
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; init; } = string.Empty;

    [JsonPropertyName("planId")]
    public string PlanId { get; init; } = string.Empty;

    [JsonPropertyName("billingCycle")]
    public string BillingCycle { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("simulated")]
    public bool Simulated { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class BillingSummaryDto
{
    [JsonPropertyName("planId")]
    public string PlanId { get; init; } = string.Empty;

    [JsonPropertyName("billingCycle")]
    public string BillingCycle { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("lastPayment")]
    public SimulatePaymentResponse? LastPayment { get; init; }

    [JsonPropertyName("simulated")]
    public bool Simulated { get; init; }
}

public sealed class BillingTransactionDto
{
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; init; } = string.Empty;

    [JsonPropertyName("planId")]
    public string PlanId { get; init; } = string.Empty;

    [JsonPropertyName("billingCycle")]
    public string BillingCycle { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("simulated")]
    public bool Simulated { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}
