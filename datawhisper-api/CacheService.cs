using System.Text.Json;
using StackExchange.Redis;

namespace DataWhisper.API
{
    public class CacheService
    {
        private readonly IDatabase _database;
        private readonly ILogger<CacheService> _logger;
        private readonly TimeSpan _defaultTtl = TimeSpan.FromHours(1);

        public CacheService(IConfiguration configuration, ILogger<CacheService> logger)
        {
            _logger = logger;
            var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

            try
            {
                var redis = ConnectionMultiplexer.Connect(redisConnectionString);
                _database = redis.GetDatabase();
                _logger.LogInformation("Redis cache service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Redis cache service");
                throw;
            }
        }

        public async Task<CachedQueryResponse?> GetCachedQueryAsync(string prompt)
        {
            try
            {
                var cacheKey = GenerateCacheKey(prompt);
                var cachedValue = await _database.StringGetAsync(cacheKey);

                if (cachedValue.HasValue)
                {
                    _logger.LogInformation("Cache hit for prompt: {Prompt}", prompt);
                    return JsonSerializer.Deserialize<CachedQueryResponse>(cachedValue.ToString()!);
                }

                _logger.LogDebug("Cache miss for prompt: {Prompt}", prompt);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cached query for prompt: {Prompt}", prompt);
                return null;
            }
        }

        public async Task SetCachedQueryAsync(string prompt, AIGenerateSqlResponse aiResponse, TimeSpan? ttl = null)
        {
            try
            {
                var cacheKey = GenerateCacheKey(prompt);
                var cacheValue = new CachedQueryResponse
                {
                    Sql = aiResponse.Sql,
                    Success = aiResponse.Success,
                    Message = aiResponse.Message,
                    CachedAt = DateTime.UtcNow
                };

                var serializedValue = JsonSerializer.Serialize(cacheValue);
                await _database.StringSetAsync(cacheKey, serializedValue, ttl ?? _defaultTtl);

                _logger.LogInformation("Cached query for prompt: {Prompt}", prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching query for prompt: {Prompt}", prompt);
            }
        }

        private string GenerateCacheKey(string prompt)
        {
            // Normalize prompt for consistent cache keys
            var normalized = prompt.ToLower().Trim();
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized));
            return $"sql_query:{Convert.ToBase64String(hash)[..16]}";
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                await _database.PingAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return false;
            }
        }
    }

    public class CachedQueryResponse
    {
        public string Sql { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CachedAt { get; set; }
    }
}