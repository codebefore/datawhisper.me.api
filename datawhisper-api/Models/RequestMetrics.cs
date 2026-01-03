namespace DataWhisper.API.Models
{
    public class RequestMetrics
    {
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public double DurationMs { get; set; }
        public bool IsDotnetRequest { get; set; } = true; // ✅ EXPLICIT MARKER
        public string? ClientIp { get; set; }
        public string? UserAgent { get; set; }
        public double? SqlExecutionTimeMs { get; set; }
        public double? AiServiceCallTimeMs { get; set; }
        public string? Error { get; set; }
        public string? ExceptionType { get; set; }
        public string? StackTrace { get; set; }
    }

    public class ErrorMetrics
    {
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public int StatusCode { get; set; }
    }

    public class AggregateMetrics
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AvgResponseTimeMs { get; set; }
        public DateTime LastReset { get; set; }
    }

    public class EndpointPerformance
    {
        public string Endpoint { get; set; } = string.Empty;
        public long RequestCount { get; set; }
        public double AvgDurationMs { get; set; }
        public int SlowRequests { get; set; }
    }

    public class SqlPerformanceMetrics
    {
        public long TotalQueries { get; set; }
        public double AvgExecutionTimeMs { get; set; }
        public long SlowQueriesCount { get; set; }
    }

    public class AiServiceLatencyMetrics
    {
        public long TotalCalls { get; set; }
        public double AvgLatencyMs { get; set; }
        public DateTime LastCallTimestamp { get; set; }
    }
}
