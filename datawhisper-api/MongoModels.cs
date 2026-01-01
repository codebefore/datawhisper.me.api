using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Concurrent;

namespace DataWhisper.API.Models
{
    public record QueryRequest
    {
        public string Prompt { get; init; } = string.Empty;
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public bool DisableCache { get; init; } = false;
        public string? Language { get; init; } = "en";
    }

    public class CachedSqlResponse
    {
        public string Sql { get; set; } = string.Empty;
        public bool IsAIGenerated { get; set; }
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    }

    // Simple cache for query results with TTL support
    public static class QueryCache
    {
        private static readonly ConcurrentDictionary<string, CachedSqlResponse> _cache = new();
        private static readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(
            int.TryParse(Environment.GetEnvironmentVariable("CACHE_TTL_MINUTES"), out var ttlMinutes)
                ? ttlMinutes
                : 5 // Default 5 minutes
        );

        public static bool TryGet(string key, out CachedSqlResponse value)
        {
            if (_cache.TryGetValue(key, out var cachedValue))
            {
                // Check if entry has expired
                if (DateTime.UtcNow - cachedValue.CachedAt > _defaultTtl)
                {
                    // Remove expired entry
                    _cache.TryRemove(key, out _);
                    value = null!;
                    return false;
                }

                value = cachedValue;
                return true;
            }

            value = null!;
            return false;
        }

        public static bool TryAdd(string key, CachedSqlResponse value)
        {
            return _cache.TryAdd(key, value);
        }

        public static void Clear()
        {
            _cache.Clear();
        }

        public static int Count => _cache.Count;

        // Clean up expired entries (can be called periodically)
        public static int CleanupExpired()
        {
            var expiredKeys = _cache.Where(kvp =>
                DateTime.UtcNow - kvp.Value.CachedAt > _defaultTtl
            ).Select(kvp => kvp.Key).ToList();

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }

            return expiredKeys.Count;
        }
    }

    [BsonIgnoreExtraElements]
    public class QueryHistoryDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("requestId")]
        public string RequestId { get; set; } = string.Empty;

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [BsonElement("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [BsonElement("sql")]
        public string Sql { get; set; } = string.Empty;

        [BsonElement("aiGenerated")]
        public bool AiGenerated { get; set; }

        [BsonElement("executionTimeMs")]
        public double ExecutionTimeMs { get; set; }

        [BsonElement("rowCount")]
        public int RowCount { get; set; }

        [BsonElement("success")]
        public bool Success { get; set; } = true;

        [BsonElement("tablesAccessed")]
        public List<string> TablesAccessed { get; set; } = new();

        [BsonElement("model")]
        public string Model { get; set; } = "unknown";

        [BsonElement("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;

        [BsonElement("userIdentifier")]
        public string UserIdentifier { get; set; } = "anonymous";

        [BsonElement("ipAddress")]
        public string? IpAddress { get; set; }

        [BsonElement("userAgent")]
        public string? UserAgent { get; set; }

        [BsonElement("starred")]
        public bool Starred { get; set; }

        [BsonElement("starredAt")]
        public DateTime? StarredAt { get; set; }
    }

    public class QueryAnalyticsDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("date")]
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;

        [BsonElement("totalQueries")]
        public int TotalQueries { get; set; }

        [BsonElement("successfulQueries")]
        public int SuccessfulQueries { get; set; }

        [BsonElement("failedQueries")]
        public int FailedQueries { get; set; }

        [BsonElement("avgExecutionTime")]
        public double AvgExecutionTime { get; set; }

        [BsonElement("topTables")]
        public List<string> TopTables { get; set; } = new();

        [BsonElement("aiGeneratedQueries")]
        public int AiGeneratedQueries { get; set; }

        [BsonElement("errorTypes")]
        public Dictionary<string, int> ErrorTypes { get; set; } = new();
    }
}