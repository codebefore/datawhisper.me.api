using System.Diagnostics;
using DataWhisper.API.Configuration;
using DataWhisper.API.Models;
using DataWhisper.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataWhisper.API.Middleware
{
    public class RequestMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRedisMetricsService _metricsService;
        private readonly ILogger<RequestMonitoringMiddleware> _logger;
        private readonly MonitoringConfiguration _config;

        public RequestMonitoringMiddleware(
            RequestDelegate next,
            IRedisMetricsService metricsService,
            ILogger<RequestMonitoringMiddleware> logger,
            IOptions<MonitoringConfiguration> config)
        {
            _next = next;
            _metricsService = metricsService;
            _logger = logger;
            _config = config.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if path should be excluded
            if (_config.ExcludePaths.Contains(context.Request.Path.Value ?? ""))
            {
                await _next(context);
                return;
            }

            var requestId = context.TraceIdentifier;
            var startTime = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            // Store request metadata in HttpContext for later access
            context.Items["RequestId"] = requestId;
            context.Items["StartTime"] = startTime;

            try
            {
                await _next(context);

                stopwatch.Stop();
                var duration = stopwatch.ElapsedMilliseconds;
                var statusCode = context.Response.StatusCode;

                var metrics = new RequestMetrics
                {
                    RequestId = requestId,
                    Timestamp = startTime,
                    Endpoint = context.Request.Path.Value ?? "",
                    Method = context.Request.Method,
                    StatusCode = statusCode,
                    DurationMs = duration,
                    IsDotnetRequest = true, // ✅ EXPLICIT MARKER
                    ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    SqlExecutionTimeMs = GetSqlExecutionTime(context),
                    AiServiceCallTimeMs = GetAiServiceCallTime(context)
                };

                // ✅ ASYNC NON-BLOCKING: Don't block request pipeline
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _metricsService.StoreRequestMetricsAsync(metrics);
                        await _metricsService.UpdateAggregateMetricsAsync(statusCode, duration);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to store metrics asynchronously");
                    }
                });

                // Add headers for tracing
                context.Response.Headers.Append("X-Request-ID", requestId);
                context.Response.Headers.Append("X-DotNet-Request", "true"); // ✅ HEADER MARKER

                // Log slow requests
                if (duration > _config.SlowRequestThresholdMs)
                {
                    _logger.LogWarning("🐌 Slow request detected: {Method} {Path} - {Duration}ms",
                        context.Request.Method, context.Request.Path, duration);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var errorMetrics = new ErrorMetrics
                {
                    RequestId = requestId,
                    Timestamp = startTime,
                    Endpoint = context.Request.Path.Value ?? "",
                    Error = ex.Message,
                    ExceptionType = ex.GetType().Name,
                    StackTrace = _config.IncludeStackTrace ? ex.StackTrace : null,
                    StatusCode = 500
                };

                // ✅ ASYNC NON-BLOCKING: Store error metrics
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _metricsService.StoreErrorAsync(errorMetrics);
                        await _metricsService.UpdateAggregateMetricsAsync(500, stopwatch.ElapsedMilliseconds);
                    }
                    catch (Exception redisEx)
                    {
                        _logger.LogError(redisEx, "Failed to store error metrics asynchronously");
                    }
                });

                // Log the exception
                _logger.LogError(ex, "💥 Unhandled exception in request {RequestId}", requestId);

                throw; // Re-throw to let the global exception handler deal with it
            }
        }

        private double? GetSqlExecutionTime(HttpContext context)
        {
            if (context.Items.TryGetValue("SqlExecutionTime", out var sqlTime) && sqlTime is long time)
            {
                return time;
            }
            return null;
        }

        private double? GetAiServiceCallTime(HttpContext context)
        {
            if (context.Items.TryGetValue("AiServiceCallTime", out var aiTime) && aiTime is long time)
            {
                return time;
            }
            return null;
        }
    }
}
