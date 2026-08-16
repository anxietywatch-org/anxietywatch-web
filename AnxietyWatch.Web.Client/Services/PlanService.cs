using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Api;

namespace AnxietyWatch.Web.Client.Services;

public interface IPlanService
{
    Task<IReadOnlyList<PlanDto>> GetPlansAsync(CancellationToken cancellationToken = default);
    bool IsCurrentPlan(PlanDto plan, string? currentPlanId);
}

public sealed class PlanService(HttpClient http, JsonSerializerOptions jsonOptions) : IPlanService
{
    public async Task<IReadOnlyList<PlanDto>> GetPlansAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("api/plans", cancellationToken);
        return await response.ReadApiAsync<IReadOnlyList<PlanDto>>(jsonOptions, cancellationToken);
    }

    public bool IsCurrentPlan(PlanDto plan, string? currentPlanId) =>
        string.Equals(plan.Id, currentPlanId, StringComparison.OrdinalIgnoreCase);
}
