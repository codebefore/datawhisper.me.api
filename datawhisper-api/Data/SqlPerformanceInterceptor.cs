using System.Diagnostics;
using DataWhisper.API.Services;
using Microsoft.Extensions.Logging;

namespace DataWhisper.API.Data
{
    /// <summary>
    /// Helper class for tracking SQL query performance when using Dapper/Npgsql.
    /// Since this project uses Dapper (not Entity Framework Core), we provide
    /// a static helper that can be used to wrap SQL queries and track execution time.
    /// </summary>
    public static class SqlPerformanceTracker
    {
        private static IRedisMetricsService? _metricsService;
        private static ILogger? _logger;
        private static bool _isInitialized = false;

        public static void Initialize(IRedisMetricsService metricsService, ILogger logger)
        {
            _metricsService = metricsService;
            _logger = logger;
            _isInitialized = true;
        }

        /// <summary>
        /// Track SQL execution time and store metrics asynchronously
        /// </summary>
        public static async Task<T> TrackQueryAsync<T>(
            Func<Task<T>> queryFunc,
            HttpContext? httpContext = null,
            string? queryName = null)
        {
            if (!_isInitialized)
            {
                return await queryFunc();
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await queryFunc();
                stopwatch.Stop();

                var executionTime = stopwatch.ElapsedMilliseconds;

                // Store SQL metrics asynchronously (non-blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_metricsService != null)
                        {
                            await _metricsService.UpdateSqlPerformanceMetricsAsync(executionTime);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to store SQL performance metrics");
                    }
                });

                // Store in HttpContext for request-level tracking
                if (httpContext != null)
                {
                    httpContext.Items["SqlExecutionTime"] = executionTime;
                }

                return result;
            }
            catch
            {
                stopwatch.Stop();
                throw;
            }
        }

        /// <summary>
        /// Track SQL execution time for synchronous queries
        /// </summary>
        public static T TrackQuery<T>(
            Func<T> queryFunc,
            HttpContext? httpContext = null,
            string? queryName = null)
        {
            if (!_isInitialized)
            {
                return queryFunc();
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = queryFunc();
                stopwatch.Stop();

                var executionTime = stopwatch.ElapsedMilliseconds;

                // Store SQL metrics asynchronously (non-blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_metricsService != null)
                        {
                            await _metricsService.UpdateSqlPerformanceMetricsAsync(executionTime);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to store SQL performance metrics");
                    }
                });

                // Store in HttpContext for request-level tracking
                if (httpContext != null)
                {
                    httpContext.Items["SqlExecutionTime"] = executionTime;
                }

                return result;
            }
            catch
            {
                stopwatch.Stop();
                throw;
            }
        }
    }
}
