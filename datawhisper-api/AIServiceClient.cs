using System.Text.Json;

namespace DataWhisper.API
{
    public class AIServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIServiceClient> _logger;

        public AIServiceClient(HttpClient httpClient, ILogger<AIServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Configure AI service base URL (use Docker service name in containers)
            var aiServiceUrl = Environment.GetEnvironmentVariable("AI_SERVICE_URL") ?? "http://datawhisper-ai:5001";
            _httpClient.BaseAddress = new Uri(aiServiceUrl);
        }

        public async Task<AIGenerateSqlResponse?> GenerateSqlAsync(string prompt, string? language = "en")
        {
            try
            {
                _logger.LogInformation("🤖 AI Service Request - Prompt: {Prompt}, Language: {Language}", prompt, language);

                var startTime = DateTime.UtcNow;
                var request = new { prompt = prompt, language = language };
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });

                // COMPREHENSIVE LOGGING: Log complete request details
                _logger.LogInformation("🔍 [AI_CLIENT_REQUEST] Sending JSON to AI service: {Json}", json);
                _logger.LogDebug("📤 AI Request JSON: {Json}", json);

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // COMPREHENSIVE LOGGING: Log HTTP call details
                _logger.LogInformation("🔍 [AI_CLIENT_HTTP_CALL] Calling AI service at {BaseAddress}/api/generate-sql with Content-Type: {ContentType}",
                    _httpClient.BaseAddress, "application/json");
                _logger.LogDebug("📡 Calling AI service at {BaseAddress}/api/generate-sql", _httpClient.BaseAddress);

                var response = await _httpClient.PostAsync("/api/generate-sql", content);
                var requestDuration = DateTime.UtcNow - startTime;

                // COMPREHENSIVE LOGGING: Log HTTP response details
                _logger.LogInformation("🔍 [AI_CLIENT_HTTP_RESPONSE] Received response - Status: {StatusCode}, Duration: {Duration}ms, Headers: {Headers}",
                    response.StatusCode, requestDuration.TotalMilliseconds, string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")));
                _logger.LogInformation("📨 AI Service Response - Status: {StatusCode}, Duration: {Duration}ms",
                    response.StatusCode, requestDuration.TotalMilliseconds);

                var responseJson = await response.Content.ReadAsStringAsync();

                // Even for non-200 status codes, check if we got valid JSON response
                if (!response.IsSuccessStatusCode && string.IsNullOrEmpty(responseJson))
                {
                    // No content in error response - actual failure
                    _logger.LogWarning("🔍 [AI_CLIENT_ERROR_RESPONSE] Error Status: {StatusCode}, No content",
                        response.StatusCode);
                    _logger.LogWarning("❌ AI Service Error - Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                if (!response.IsSuccessStatusCode && !string.IsNullOrEmpty(responseJson))
                {
                    // Got error response with JSON content - log it but continue processing
                    _logger.LogInformation("🔍 [AI_CLIENT_ERROR_RESPONSE] Error Status: {StatusCode}, Content: {ErrorContent}",
                        response.StatusCode, responseJson);
                }

                // COMPREHENSIVE LOGGING: Log raw response JSON
                _logger.LogInformation("🔍 [AI_CLIENT_RESPONSE_JSON] Raw JSON from AI service: {Json}", responseJson);
                _logger.LogDebug("📥 AI Response JSON: {Json}", responseJson);

                var aiResponse = JsonSerializer.Deserialize<AIGenerateSqlResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // COMPREHENSIVE LOGGING: Log deserialization result
                _logger.LogInformation("🔍 [AI_CLIENT_DESERIALIZATION] Deserialized response - Success: {Success}, CanConvert: {CanConvert}, SQL: '{SQL}'",
                    aiResponse?.Success, aiResponse?.CanConvert, aiResponse?.Sql);

                if (aiResponse?.Success == true)
                {
                    if (aiResponse.CanConvert)
                    {
                        _logger.LogInformation("✅ AI Service Success - Generated SQL: {Sql}", aiResponse.Sql);
                        _logger.LogInformation("📊 SQL generation completed successfully - Model: {Model}", aiResponse?.Model);
                        return aiResponse;
                    }
                    else
                    {
                        _logger.LogInformation("⚠️ AI Service - Prompt cannot be converted to SQL: {Reason}", aiResponse.Reason);
                        // Still return the response so caller can handle the invalid prompt case
                        return aiResponse;
                    }
                }
                else
                {
                    _logger.LogWarning("❌ AI Service Failed - Error: {Error}, Message: {Message}",
                        aiResponse?.Error, aiResponse?.Message);
                    return aiResponse; // Return the response so caller can display error message
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "🌐 HTTP error when calling AI service - Status: {StatusCode}",
                    ex.StatusCode);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "📄 JSON parsing error when calling AI service - Path: {Path}",
                    ex.Path);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "⏰ AI service request timeout");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error when calling AI service");
                return null;
            }
        }

        public async Task<bool> TrainSchemaAsync(string schemaDdl)
        {
            try
            {
                _logger.LogInformation("Training AI service with database schema");

                var request = new { schema_ddl = schemaDdl };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/train-schema", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Schema training failed with status code: {StatusCode}", response.StatusCode);
                    return false;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var trainResponse = JsonSerializer.Deserialize<TrainResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var success = trainResponse?.Success == true;
                if (success)
                {
                    _logger.LogInformation("Schema training completed successfully");
                }
                else
                {
                    _logger.LogWarning("Schema training failed: {Error}", trainResponse?.Error);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during schema training");
                return false;
            }
        }

        
        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking AI service health");
                return false;
            }
        }

        public async Task<AIStatusResponse?> GetStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/ai-status");

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AIStatusResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI service status");
                return null;
            }
        }
    }

    public record AIGenerateSqlResponse
    {
        public bool Success { get; init; }
        public bool CanConvert { get; init; }  // Added for single-call optimization
        public string Prompt { get; init; } = string.Empty;
        public string Sql { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string Error { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;  // Added for invalid prompts
        public string Method { get; init; } = "ai";
        public bool AiGenerated { get; init; }
        public string[] TablesAccessed { get; init; } = Array.Empty<string>();
        public string Model { get; init; } = "gpt-4o-mini";

        // Large dataset metadata
        public int? TotalRows { get; init; }
        public string[] AiSuggestions { get; init; } = Array.Empty<string>();
        public bool IsLargeDataset { get; init; }
    }

    
    public record TrainResponse
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string Error { get; init; } = string.Empty;
    }

    public record AIStatusResponse
    {
        public string Service { get; init; } = string.Empty;
        public string OpenaiClient { get; init; } = string.Empty;
        public bool OpenaiConfigured { get; init; }
        public SecurityConfig Security { get; init; } = new();
    }

    public record SecurityConfig
    {
        public string SqlValidation { get; init; } = string.Empty;
        public string[] AllowedTables { get; init; } = Array.Empty<string>();
        public string[] ForbiddenOperations { get; init; } = Array.Empty<string>();
    }

    }