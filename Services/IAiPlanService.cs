using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Services;

public interface IAiPlanService
{
    Task<AiPlanResponse> GenerateAiPlanAsync(string playerId, AiPlanGenerateRequest? request = null);
    Task<AiPlanResponse?> GetLatestAiPlanAsync(string playerId);
    Task<AiPlanHistoryResponse> GetAiPlanHistoryAsync(string playerId);
}