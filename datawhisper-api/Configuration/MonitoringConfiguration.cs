namespace DataWhisper.API.Configuration
{
    public class MonitoringConfiguration
    {
        public const string SectionName = "Monitoring";

        public string RedisConnectionString { get; set; } = "localhost:6379";
        public int RedisDbId { get; set; } = 0;
        public string RedisPassword { get; set; } = string.Empty;
        public int ConnectionPoolSize { get; set; } = 20;
        public int ConnectTimeoutMs { get; set; } = 5000;
        public int SyncTimeoutMs { get; set; } = 5000;

        // Monitoring behavior
        public List<string> ExcludePaths { get; set; } = new() { "/health", "/ping" };
        public bool IncludeStackTrace { get; set; } = false;
        public bool EnableSqlTracking { get; set; } = true;
        public bool EnableAiServiceTracking { get; set; } = true;
        public int SlowQueryThresholdMs { get; set; } = 100;
        public int SlowRequestThresholdMs { get; set; } = 1000;
    }
}
