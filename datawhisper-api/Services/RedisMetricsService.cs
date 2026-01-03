using StackExchange.Redis;
using System.Text.Json;
using DataWhisper.API.Configuration;
using DataWhisper.API.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace DataWhisper.API.Services
{
    public class RedisMetricsService : IRedisMetricsService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisMetricsService> _logger;
        private readonly MonitoringConfiguration _config;

        public RedisMetricsService(
            IConnectionMultiplexer redis,
            ILogger<RedisMetricsService> logger,
            IOptions<MonitoringConfiguration> config)
        {
            _redis = redis;
            _logger = logger;
            _config = config.Value;
        }

        public async Task<bool> StoreRequestMetricsAsync(RequestMetrics metrics)
        {
            try
            {
                var db = _redis.GetDatabase();
                var dateKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var key = $"dotnet_requests:{dateKey}";

                var field = $"{((long)(metrics.Timestamp - DateTime.UnixEpoch).TotalSeconds)}:{metrics.RequestId}";
                var json = JsonSerializer.Serialize(metrics);

                await db.HashSetAsync(key, field, json);
                await db.KeyExpireAsync(key, TimeSpan.FromDays(7));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store request metrics in Redis");
                return false;
            }
        }

        public async Task<bool> UpdateAggregateMetricsAsync(int statusCode, long durationMs)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = "dotnet_metrics:aggregate";

                var totalRequests = (long)(await db.HashGetAsync(key, "total_requests")) + 1;
                var successfulRequests = (long)(await db.HashGetAsync(key, "successful_requests"));
                var failedRequests = (long)(await db.HashGetAsync(key, "failed_requests"));

                if (statusCode >= 200 && statusCode < 400)
                    successfulRequests++;
                else
                    failedRequests++;

                // Update average response time
                var currentAvg = (double?)await db.HashGetAsync(key, "avg_response_time_ms") ?? 0;
                var newAvg = ((currentAvg * (totalRequests - 1)) + durationMs) / totalRequests;

                await db.HashSetAsync(key, new HashEntry[]
                {
                    new("total_requests", totalRequests),
                    new("successful_requests", successfulRequests),
                    new("failed_requests", failedRequests),
                    new("avg_response_time_ms", newAvg),
                    new("last_request_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update aggregate metrics in Redis");
                return false;
            }
        }

        public async Task<bool> StoreErrorAsync(ErrorMetrics error)
        {
            try
            {
                var db = _redis.GetDatabase();
                var dateKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var key = $"dotnet_errors:{dateKey}";

                var field = $"{((long)(error.Timestamp - DateTime.UnixEpoch).TotalSeconds)}:{error.RequestId}";
                var json = JsonSerializer.Serialize(error);

                await db.HashSetAsync(key, field, json);
                await db.KeyExpireAsync(key, TimeSpan.FromDays(30));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store error metrics in Redis");
                return false;
            }
        }

        public async Task<bool> UpdateSqlPerformanceMetricsAsync(long executionTimeMs)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = "dotnet_sql:performance";

                var totalQueries = (long)(await db.HashGetAsync(key, "total_queries")) + 1;
                var currentAvg = (double?)await db.HashGetAsync(key, "avg_execution_time_ms") ?? 0;
                var newAvg = ((currentAvg * (totalQueries - 1)) + executionTimeMs) / totalQueries;

                var slowQueries = (long)(await db.HashGetAsync(key, "slow_queries"));
                if (executionTimeMs > _config.SlowQueryThresholdMs)
                    slowQueries++;

                await db.HashSetAsync(key, new HashEntry[]
                {
                    new("total_queries", totalQueries),
                    new("avg_execution_time_ms", newAvg),
                    new("slow_queries", slowQueries)
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update SQL performance metrics in Redis");
                return false;
            }
        }

        public async Task<bool> UpdateAiServiceLatencyAsync(long latencyMs)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = "dotnet_ai_calls:latency";

                var totalCalls = (long)(await db.HashGetAsync(key, "total_calls")) + 1;
                var currentAvg = (double?)await db.HashGetAsync(key, "avg_latency_ms") ?? 0;
                var newAvg = ((currentAvg * (totalCalls - 1)) + latencyMs) / totalCalls;

                await db.HashSetAsync(key, new HashEntry[]
                {
                    new("total_calls", totalCalls),
                    new("avg_latency_ms", newAvg),
                    new("last_call_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update AI service latency metrics in Redis");
                return false;
            }
        }

        public async Task<AggregateMetrics?> GetAggregateMetricsAsync()
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = "dotnet_metrics:aggregate";

                var values = await db.HashGetAllAsync(key);

                if (values.Length == 0)
                {
                    return new AggregateMetrics
                    {
                        TotalRequests = 0,
                        SuccessfulRequests = 0,
                        FailedRequests = 0,
                        AvgResponseTimeMs = 0,
                        LastReset = DateTime.UtcNow
                    };
                }

                var dict = values.ToDictionary();

                return new AggregateMetrics
                {
                    TotalRequests = (int)dict.TryGetValue("total_requests", out var total) ? total : 0,
                    SuccessfulRequests = (int)dict.TryGetValue("successful_requests", out var success) ? success : 0,
                    FailedRequests = (int)dict.TryGetValue("failed_requests", out var failed) ? failed : 0,
                    AvgResponseTimeMs = (double)dict.TryGetValue("avg_response_time_ms", out var avg) ? avg : 0,
                    LastReset = DateTime.UnixEpoch.AddSeconds((double)(dict.TryGetValue("last_request_timestamp", out var ts) ? ts : 0))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get aggregate metrics from Redis");
                return null;
            }
        }

        public async Task<List<RequestMetrics>> GetDailyMetricsAsync(DateTime date)
        {
            try
            {
                var db = _redis.GetDatabase();
                var dateKey = date.ToString("yyyy-MM-dd");
                var key = $"dotnet_requests:{dateKey}";

                var values = await db.HashValuesAsync(key);
                var metrics = new List<RequestMetrics>();

                foreach (var value in values)
                {
                    if (value.HasValue)
                    {
                        var metric = JsonSerializer.Deserialize<RequestMetrics>(value!);
                        if (metric != null)
                        {
                            metrics.Add(metric);
                        }
                    }
                }

                return metrics.OrderByDescending(m => m.Timestamp).Take(100).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get daily metrics from Redis");
                return new List<RequestMetrics>();
            }
        }

        public async Task<List<EndpointPerformance>> GetEndpointPerformanceAsync()
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = "dotnet_performance:endpoint";

                var endpoints = await db.SortedSetRangeByRankWithScoresAsync(key, order: Order.Descending, take: 20);
                var performanceList = new List<EndpointPerformance>();

                foreach (var endpoint in endpoints)
                {
                    if (endpoint.Element.HasValue)
                    {
                        var endpointName = endpoint.Element.ToString();
                        var hashKey = $"dotnet_endpoint:{endpointName}";

                        var count = (long)(await db.HashGetAsync(hashKey, "count"));
                        var avgDuration = (double)(await db.HashGetAsync(hashKey, "avg_duration"));
                        var slowRequests = (int)(await db.HashGetAsync(hashKey, "slow_requests"));

                        performanceList.Add(new EndpointPerformance
                        {
                            Endpoint = endpointName!,
                            RequestCount = count,
                            AvgDurationMs = avgDuration,
                            SlowRequests = slowRequests
                        });
                    }
                }

                return performanceList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get endpoint performance from Redis");
                return new List<EndpointPerformance>();
            }
        }

        public async Task<SqlPerformanceMetrics?> GetSqlPerformanceMetricsAsync()
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = "dotnet_sql:performance";

                var values = await db.HashGetAllAsync(key);

                if (values.Length == 0)
                {
                    return new SqlPerformanceMetrics
                    {
                        TotalQueries = 0,
                        AvgExecutionTimeMs = 0,
                        SlowQueriesCount = 0
                    };
                }

                var dict = values.ToDictionary();

                return new SqlPerformanceMetrics
                {
                    TotalQueries = (long)dict.TryGetValue("total_queries", out var total) ? total : 0,
                    AvgExecutionTimeMs = (double)dict.TryGetValue("avg_execution_time_ms", out var avg) ? avg : 0,
                    SlowQueriesCount = (long)dict.TryGetValue("slow_queries", out var slow) ? slow : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get SQL performance metrics from Redis");
                return null;
            }
        }

        public async Task<AiServiceLatencyMetrics?> GetAiServiceLatencyMetricsAsync()
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = "dotnet_ai_calls:latency";

                var values = await db.HashGetAllAsync(key);

                if (values.Length == 0)
                {
                    return new AiServiceLatencyMetrics
                    {
                        TotalCalls = 0,
                        AvgLatencyMs = 0,
                        LastCallTimestamp = DateTime.UtcNow
                    };
                }

                var dict = values.ToDictionary();

                return new AiServiceLatencyMetrics
                {
                    TotalCalls = (long)dict.TryGetValue("total_calls", out var total) ? total : 0,
                    AvgLatencyMs = (double)dict.TryGetValue("avg_latency_ms", out var avg) ? avg : 0,
                    LastCallTimestamp = DateTime.UnixEpoch.AddSeconds((double)(dict.TryGetValue("last_call_timestamp", out var ts) ? ts : 0))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AI service latency metrics from Redis");
                return null;
            }
        }
    }
}
