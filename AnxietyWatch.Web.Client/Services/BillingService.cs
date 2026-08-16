using System.Net.Http.Json;
using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Api;

namespace AnxietyWatch.Web.Client.Services;

public interface IBillingService
{
    Task<SimulatedPaymentDto> SimulatePaymentAsync(SimulatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<BillingSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public sealed class BillingService(HttpClient http, JsonSerializerOptions jsonOptions) : IBillingService
{
    public async Task<SimulatedPaymentDto> SimulatePaymentAsync(SimulatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/billing/simulate-payment", request, jsonOptions, cancellationToken);
        return await response.ReadApiAsync<SimulatedPaymentDto>(jsonOptions, cancellationToken);
    }

    public async Task<BillingSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("api/billing/summary", cancellationToken);
        return await response.ReadApiAsync<BillingSummaryDto>(jsonOptions, cancellationToken);
    }
}
