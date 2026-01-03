using DataWhisper.API.Models;

namespace DataWhisper.API.Services
{
    public interface IRedisMetricsService
    {
        Task<bool> StoreRequestMetricsAsync(RequestMetrics metrics);
        Task<bool> UpdateAggregateMetricsAsync(int statusCode, long durationMs);
        Task<bool> StoreErrorAsync(ErrorMetrics error);
        Task<bool> UpdateSqlPerformanceMetricsAsync(long executionTimeMs);
        Task<bool> UpdateAiServiceLatencyAsync(long latencyMs);
        Task<AggregateMetrics?> GetAggregateMetricsAsync();
        Task<List<RequestMetrics>> GetDailyMetricsAsync(DateTime date);
        Task<List<EndpointPerformance>> GetEndpointPerformanceAsync();
        Task<SqlPerformanceMetrics?> GetSqlPerformanceMetricsAsync();
        Task<AiServiceLatencyMetrics?> GetAiServiceLatencyMetricsAsync();
    }
}
