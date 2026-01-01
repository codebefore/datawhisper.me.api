using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using DataWhisper.API.Models;
using MongoDB.Driver;

namespace DataWhisper.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly ILogger<QueryController> _logger;
        private readonly string _connectionString;
        private readonly AIServiceClient _aiServiceClient;
        private readonly IMongoDatabase _mongoDatabase;
        private readonly SqlFixerService _sqlFixerService;

        private static string AddPaginationToSql(string sql, int page = 1, int pageSize = 10, int? totalRows = null, int? threshold = null, int? openAiLimit = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sql))
                    return sql;

                sql = sql.Trim();

                // Get threshold from environment or use default of 10
                int largeDatasetThreshold = threshold ?? 10;

                // Check if we need pagination based on expected row count
                bool needsPagination = true;

                if (totalRows.HasValue)
                {
                    // We know the expected row count from AI response
                    needsPagination = totalRows.Value > largeDatasetThreshold;
                }
                else
                {
                    // We don't know row count, check if SQL has LIMIT
                    var limitPos = sql.IndexOf("LIMIT", StringComparison.OrdinalIgnoreCase);
                    if (limitPos >= 0)
                    {
                        // Extract LIMIT value
                        var afterLimit = sql.Substring(limitPos + 5).Trim();
                        var limitMatch = System.Text.RegularExpressions.Regex.Match(afterLimit, @"^\d+");
                        if (limitMatch.Success && int.TryParse(limitMatch.Value, out int openaiLimit))
                        {
                            // If OpenAI LIMIT is within threshold, no pagination needed
                            needsPagination = openaiLimit > largeDatasetThreshold;

                            if (!needsPagination)
                            {
                                // Small LIMIT (e.g., "LIMIT 5") within threshold - skip pagination
                                return sql;
                            }
                            // Large LIMIT - remove it and apply our pagination
                            sql = sql.Substring(0, limitPos).Trim();
                        }
                        else
                        {
                            // LIMIT without valid number - remove it
                            sql = sql.Substring(0, limitPos).Trim();
                        }
                    }
                    // No LIMIT - assume we need pagination
                }

                if (!needsPagination)
                {
                    // Result set is small, return SQL as-is
                    return sql;
                }

                // Apply pagination for large result sets
                // Calculate offset
                var offset = (page - 1) * pageSize;

                // Calculate actual LIMIT for this page
                // If OpenAI specified a LIMIT, respect it by calculating remaining rows
                int actualLimit = pageSize;
                if (openAiLimit.HasValue)
                {
                    int remainingRows = openAiLimit.Value - offset;
                    actualLimit = Math.Max(0, Math.Min(pageSize, remainingRows));
                }

                // If no rows remaining (page beyond OpenAI limit), return 0 results
                if (actualLimit <= 0)
                {
                    actualLimit = 0;
                }

                // Ensure SQL ends with semicolon
                if (!sql.EndsWith(";"))
                {
                    sql += ";";
                }

                // Add pagination (PostgreSQL syntax: LIMIT ... OFFSET ...)
                var paginatedSql = $"{sql.TrimEnd(';')} LIMIT {actualLimit} OFFSET {offset};";
                return paginatedSql;
            }
            catch
            {
                return sql;
            }
        }

        public QueryController(
            ILogger<QueryController> logger,
            IConfiguration configuration,
            AIServiceClient aiServiceClient,
            IMongoDatabase mongoDatabase,
            SqlFixerService sqlFixerService)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _aiServiceClient = aiServiceClient;
            _mongoDatabase = mongoDatabase;
            _sqlFixerService = sqlFixerService;
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteQuery([FromBody] QueryRequest request)
        {
            var requestStartTime = DateTime.UtcNow;
            Guid requestId = Guid.NewGuid();
            string? errorMessage = null;
            string sql = string.Empty;

            // Step 1: Validate request
            var validationResult = ValidateRequest(request, requestId);
            if (validationResult != null)
            {
                // Log validation errors to MongoDB
                _ = Task.Run(() => LogErrorToHistory(requestId, request.Prompt, sql, "Validation failed: " + (validationResult as ObjectResult)?.Value?.ToString()));
                return validationResult;
            }

            // Step 2: Get or generate SQL
            var sqlResult = await GetOrGenerateSqlAsync(request.Prompt, requestId, request.DisableCache, request.Language);
            if (!sqlResult.IsSuccess)
            {
                // Log SQL generation failure to MongoDB
                errorMessage = "SQL generation failed";
                _ = Task.Run(() => LogErrorToHistory(requestId, request.Prompt, sql, errorMessage));

                if (sqlResult.ErrorResponse != null)
                    return Ok(sqlResult.ErrorResponse);
                return Ok(new { success = false, message = errorMessage });
            }

            // Step 3: Validate SQL
            if (string.IsNullOrWhiteSpace(sqlResult.Sql))
            {
                _logger.LogWarning("[{RequestId}] Empty SQL provided - cannot execute", requestId);
                errorMessage = "AI could not generate a valid SQL query for this prompt";
                // Log to MongoDB
                _ = Task.Run(() => LogErrorToHistory(requestId, request.Prompt, sql, errorMessage));

                return Ok(new
                {
                    success = false,
                    message = errorMessage,
                    prompt = request.Prompt,
                    sql = "",
                    data = new List<Dictionary<string, object>>(),
                    rowCount = 0,
                    timestamp = DateTime.UtcNow
                });
            }

            sql = sqlResult.Sql;

            // Log AI generated SQL
            _logger.LogInformation("[{RequestId}] 🤖 AI Generated SQL: {Sql}", requestId, sqlResult.Sql);

            // Step 3.5: Fix SQL if needed
            var fixedSql = _sqlFixerService.FixSql(sqlResult.Sql);
            if (fixedSql != sqlResult.Sql)
            {
                _logger.LogInformation("[{RequestId}] 🔧 SQL was fixed by SqlFixerService", requestId);
                sqlResult = new SqlGenerationResult
                {
                    IsSuccess = true,
                    Sql = fixedSql,
                    IsAIGenerated = sqlResult.IsAIGenerated,
                    AiResponse = sqlResult.AiResponse,
                    TotalRows = sqlResult.TotalRows
                };
                sql = fixedSql;
            }

            // Step 4: Apply pagination
            var threshold = int.Parse(Environment.GetEnvironmentVariable("LARGE_DATASET_THRESHOLD") ?? "10");
            var paginationResult = ApplyPagination(sqlResult.Sql, request.Page, request.PageSize, sqlResult.OpenAiLimit, sqlResult.TotalRows, threshold);

            // Step 5: Execute query
            var executionResult = await ExecuteQueryAsync(paginationResult.PaginatedSql, paginationResult.OriginalSql, request);
            if (executionResult == null)
            {
                errorMessage = "Query execution failed";
                // Log execution failure to MongoDB
                _ = Task.Run(() => LogErrorToHistory(requestId, request.Prompt, sql, errorMessage));
                return Ok(new { success = false, message = errorMessage, prompt = request.Prompt });
            }

            // Step 6: Get AI suggestions
            var suggestions = await GetSuggestionsAsync(request.Prompt, sqlResult.AiResponse);

            // Step 7: Build response
            var totalDuration = DateTime.UtcNow - requestStartTime;

            // Step 8: Log to history (non-critical)
            try
            {
                await LogQueryToHistory(_logger, _mongoDatabase, HttpContext, requestId, request.Prompt, sqlResult.Sql,
                    totalDuration.TotalMilliseconds, executionResult.Data.Count, sqlResult.IsAIGenerated, sqlResult.AiResponse, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{RequestId}] ⚠️ Failed to log query to history", requestId);
            }

            // Step 9: Return response
            return Ok(BuildResponse(request.Prompt, paginationResult, executionResult, suggestions, request));
        }

        #region Private Helper Methods

        private IActionResult? ValidateRequest(QueryRequest request, Guid requestId)
        {
            if (request == null)
            {
                _logger.LogWarning("[{RequestId}] ❌ Invalid request - null or empty", requestId);
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request",
                    requestId = requestId
                });
            }

            if (request.Page < 1)
            {
                _logger.LogWarning("[{RequestId}] ❌ Invalid page number: {Page}", requestId, request.Page);
                return BadRequest(new
                {
                    success = false,
                    message = "Page number must be greater than 0",
                    requestId = requestId
                });
            }

            if (request.PageSize < 1 || request.PageSize > 1000)
            {
                _logger.LogWarning("[{RequestId}] ❌ Invalid page size: {PageSize}", requestId, request.PageSize);
                return BadRequest(new
                {
                    success = false,
                    message = "Page size must be between 1 and 1000",
                    requestId = requestId
                });
            }

            return null;
        }

        private async Task<SqlGenerationResult> GetOrGenerateSqlAsync(string prompt, Guid requestId, bool disableCache = false, string? language = "en")
        {
            var cacheKey = $"sql_cache:{prompt.ToLower().Trim()}";

            // Check cache first (unless disabled)
            if (!disableCache)
            {
                if (QueryCache.TryGet(cacheKey, out var cachedResponse))
                {
                    return new SqlGenerationResult
                    {
                        IsSuccess = true,
                        Sql = cachedResponse.Sql,
                        IsAIGenerated = cachedResponse.IsAIGenerated
                    };
                }
            }

            // Call AI service
            if (_aiServiceClient == null)
            {
                _logger.LogError("[{RequestId}] ❌ AI Service client not configured", requestId);
                return new SqlGenerationResult
                {
                    IsSuccess = false,
                    ErrorResponse = new
                    {
                        success = false,
                        message = "AI service not available - cannot generate SQL",
                        prompt = prompt,
                        timestamp = DateTime.UtcNow,
                        requestId = requestId
                    }
                };
            }

            var aiResponse = await _aiServiceClient.GenerateSqlAsync(prompt, language);

            if (aiResponse != null && aiResponse.CanConvert && !string.IsNullOrEmpty(aiResponse.Sql))
            {
                // Cache the successful response
                QueryCache.TryAdd(cacheKey, new CachedSqlResponse
                {
                    Sql = aiResponse.Sql,
                    IsAIGenerated = true,
                    CachedAt = DateTime.UtcNow
                });

                return new SqlGenerationResult
                {
                    IsSuccess = true,
                    Sql = aiResponse.Sql,
                    IsAIGenerated = true,
                    AiResponse = aiResponse,
                    TotalRows = aiResponse.TotalRows
                };
            }

            // AI service failed
            if (aiResponse != null && !string.IsNullOrEmpty(aiResponse.Message))
            {
                return new SqlGenerationResult
                {
                    IsSuccess = false,
                    ErrorResponse = new
                    {
                        success = false,
                        message = aiResponse.Message,
                        prompt = prompt,
                        timestamp = DateTime.UtcNow,
                        requestId = requestId
                    }
                };
            }

            _logger.LogWarning("[{RequestId}] ❌ AI Service failed: {Error}", requestId, aiResponse?.Error);
            return new SqlGenerationResult
            {
                IsSuccess = false,
                ErrorResponse = new
                {
                    success = false,
                    message = "AI service not available - cannot generate SQL",
                    prompt = prompt,
                    timestamp = DateTime.UtcNow,
                    requestId = requestId
                }
            };
        }

        private PaginationResult ApplyPagination(string sql, int page, int pageSize, int? existingLimit, int? totalRows, int threshold)
        {
            var originalSql = sql;
            string paginatedSql;
            int? openAiLimit = existingLimit;

            var limitPos = sql.IndexOf("LIMIT", StringComparison.OrdinalIgnoreCase);
            if (limitPos >= 0)
            {
                var afterLimit = sql.Substring(limitPos + 5).Trim();
                var limitMatch = System.Text.RegularExpressions.Regex.Match(afterLimit, @"^\d+");
                if (limitMatch.Success && int.TryParse(limitMatch.Value, out int extractedLimit))
                {
                    openAiLimit = extractedLimit;
                    if (extractedLimit <= threshold)
                    {
                        paginatedSql = sql;
                    }
                    else
                    {
                        paginatedSql = AddPaginationToSql(sql, page, pageSize, null, threshold, openAiLimit);
                    }
                }
                else
                {
                    paginatedSql = AddPaginationToSql(sql, page, pageSize, null, threshold, openAiLimit);
                }
            }
            else
            {
                paginatedSql = AddPaginationToSql(sql, page, pageSize, totalRows, threshold, openAiLimit);
            }

            return new PaginationResult
            {
                OriginalSql = originalSql,
                PaginatedSql = paginatedSql,
                OpenAiLimit = openAiLimit
            };
        }

        private async Task<QueryExecutionResult?> ExecuteQueryAsync(string paginatedSql, string originalSql, QueryRequest request)
        {
            int totalRows = await GetTotalRowCountAsync(originalSql);

            using var db = new NpgsqlConnection(_connectionString);
            await db.OpenAsync();

            var data = new List<Dictionary<string, object>>();
            var result = await db.ExecuteReaderAsync(paginatedSql);

            while (await result.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < result.FieldCount; i++)
                {
                    row[result.GetName(i)] = result.GetValue(i);
                }
                data.Add(row);
            }

            // Fallback total rows calculation if count query failed
            if (totalRows == 0)
            {
                if (data.Count == request.PageSize)
                    totalRows = request.Page * request.PageSize + 1;
                else
                    totalRows = (request.Page - 1) * request.PageSize + data.Count;
            }

            return new QueryExecutionResult
            {
                Data = data,
                TotalRows = totalRows
            };
        }

        private async Task<int> GetTotalRowCountAsync(string originalSql)
        {
            try
            {
                using var countDb = new NpgsqlConnection(_connectionString);
                await countDb.OpenAsync();
                var countSql = $"SELECT COUNT(*) FROM ({originalSql.Replace(";", "")}) AS subquery";
                return await countDb.QueryFirstOrDefaultAsync<int>(countSql);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private async Task<List<string>> GetSuggestionsAsync(string prompt, AIGenerateSqlResponse? aiResponse)
        {
            if (aiResponse?.AiSuggestions != null && aiResponse.AiSuggestions.Length > 0)
            {
                return aiResponse.AiSuggestions.ToList();
            }

            var aiServiceClient = HttpContext.RequestServices.GetService<AIServiceClient>();
            if (aiServiceClient == null)
                return new List<string>();

            try
            {
                var suggestionResponse = await aiServiceClient.GenerateSqlAsync(prompt);
                if (suggestionResponse?.AiSuggestions != null)
                {
                    return suggestionResponse.AiSuggestions.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to get suggestions from AI service");
            }

            return new List<string>();
        }

        private object BuildResponse(string prompt, PaginationResult pagination, QueryExecutionResult execution, List<string> suggestions, QueryRequest request)
        {
            return new
            {
                success = true,
                message = "Query executed successfully",
                prompt = prompt,
                sql = pagination.OriginalSql,
                paginatedSql = pagination.PaginatedSql,
                data = execution.Data,
                rowCount = execution.Data.Count,
                timestamp = DateTime.UtcNow,
                ai_suggestions = suggestions,
                pagination = new
                {
                    page = request.Page,
                    pageSize = request.PageSize,
                    hasMore = (request.Page * request.PageSize) < execution.TotalRows,
                    totalRows = execution.TotalRows,
                    totalPages = (int)Math.Ceiling((double)execution.TotalRows / request.PageSize)
                }
            };
        }

        #endregion


        private static async Task LogQueryToHistory(ILogger logger, IMongoDatabase database, HttpContext context, Guid requestId, string prompt, string sql,
            double executionTimeMs, int rowCount, bool aiGenerated, AIGenerateSqlResponse? aiResponse, string? errorMessage)
        {
            try
            {
                var tablesAccessed = new List<string>();
                if (aiResponse?.TablesAccessed != null)
                {
                    tablesAccessed.AddRange(aiResponse.TablesAccessed);
                }

                var document = new QueryHistoryDocument
                {
                    RequestId = requestId.ToString(),
                    Prompt = prompt,
                    Sql = sql,
                    AiGenerated = aiGenerated,
                    ExecutionTimeMs = executionTimeMs,
                    RowCount = rowCount,
                    Success = string.IsNullOrEmpty(errorMessage),
                    TablesAccessed = tablesAccessed,
                    Model = aiResponse?.Model ?? "unknown",
                    ErrorMessage = errorMessage ?? string.Empty,
                    UserIdentifier = context.User?.Identity?.Name ?? "anonymous",
                    IpAddress = context.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request?.Headers["User-Agent"].ToString()
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var collection = database.GetCollection<QueryHistoryDocument>("query_history");
                await collection.InsertOneAsync(document, cancellationToken: cts.Token);
            }
            catch (Exception mongoEx)
            {
                logger.LogWarning(mongoEx, "[{RequestId}] ⚠️ Failed to log query to MongoDB: {Error}", requestId, mongoEx.Message);
            }
        }

        private async Task LogErrorToHistory(Guid requestId, string prompt, string sql, string errorMessage)
        {
            try
            {
                var document = new QueryHistoryDocument
                {
                    RequestId = requestId.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Prompt = prompt,
                    Sql = sql,
                    AiGenerated = false,
                    ExecutionTimeMs = 0,
                    RowCount = 0,
                    Success = false,
                    TablesAccessed = new List<string>(),
                    Model = "unknown",
                    ErrorMessage = errorMessage,
                    UserIdentifier = HttpContext.User?.Identity?.Name ?? "anonymous",
                    IpAddress = HttpContext.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request?.Headers["User-Agent"].ToString()
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var collection = _mongoDatabase.GetCollection<QueryHistoryDocument>("query_history");
                await collection.InsertOneAsync(document, cancellationToken: cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{RequestId}] ⚠️ Failed to log error to MongoDB: {Error}", requestId, ex.Message);
            }
        }
    }
}
