using System.Text.Json.Serialization;

namespace AnxietyWatch.Web.Client.Models.Api;

public sealed record SimulatePaymentRequest(
    [property: JsonPropertyName("planId")] string PlanId,
    [property: JsonPropertyName("billingCycle")] string BillingCycle);

public sealed class SimulatedPaymentDto
{
    [JsonPropertyName("transactionId")] public string TransactionId { get; init; } = string.Empty;
    [JsonPropertyName("planId")] public string PlanId { get; init; } = string.Empty;
    [JsonPropertyName("billingCycle")] public string BillingCycle { get; init; } = string.Empty;
    [JsonPropertyName("amount")] public decimal Amount { get; init; }
    [JsonPropertyName("currency")] public string Currency { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("simulated")] public bool Simulated { get; init; }
    [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt { get; init; }
}

public sealed class BillingSummaryDto
{
    [JsonPropertyName("planId")] public string PlanId { get; init; } = string.Empty;
    [JsonPropertyName("billingCycle")] public string BillingCycle { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("lastPayment")] public SimulatedPaymentDto? LastPayment { get; init; }
    [JsonPropertyName("transactions")] public IReadOnlyList<SimulatedPaymentDto> Transactions { get; init; } = [];
    [JsonPropertyName("simulated")] public bool Simulated { get; init; }
}

public sealed class DowngradeToFreeResponse
{
    [JsonPropertyName("planId")] public string PlanId { get; init; } = string.Empty;
    [JsonPropertyName("previousPlanId")] public string PreviousPlanId { get; init; } = string.Empty;
    [JsonPropertyName("changed")] public bool Changed { get; init; }
    [JsonPropertyName("downgradedAt")] public DateTimeOffset? DowngradedAt { get; init; }
}

public sealed class TokenQuotaDto
{
    [JsonPropertyName("limit")] public int Limit { get; init; }
    [JsonPropertyName("used")] public int Used { get; init; }
    [JsonPropertyName("remaining")] public int Remaining { get; init; }
}
