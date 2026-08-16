using System.Net.Http.Json;
using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Api;

namespace AnxietyWatch.Web.Client.Services;

public interface IBillingService
{
    Task<SimulatePaymentResponse> SimulatePaymentAsync(
        SimulatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<BillingSummaryDto> GetBillingSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BillingTransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken = default);
}

public sealed class BillingService(HttpClient http, JsonSerializerOptions jsonOptions) : IBillingService
{
    public async Task<SimulatePaymentResponse> SimulatePaymentAsync(
        SimulatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            "api/billing/simulate-payment",
            request,
            jsonOptions,
            cancellationToken);
        return await response.ReadApiAsync<SimulatePaymentResponse>(jsonOptions, cancellationToken);
    }

    public async Task<BillingSummaryDto> GetBillingSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("api/billing/summary", cancellationToken);
        return await response.ReadApiAsync<BillingSummaryDto>(jsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<BillingTransactionDto>> GetTransactionsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("api/billing/transactions", cancellationToken);
        return await response.ReadApiAsync<IReadOnlyList<BillingTransactionDto>>(jsonOptions, cancellationToken);
    }
}
