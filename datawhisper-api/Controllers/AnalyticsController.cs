using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using DataWhisper.API.Models;
using Microsoft.AspNetCore.RateLimiting;

namespace DataWhisper.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("AnalyticsPolicy")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ILogger<AnalyticsController> _logger;
        private readonly IMongoDatabase _mongoDatabase;

        public AnalyticsController(
            ILogger<AnalyticsController> logger,
            IMongoDatabase mongoDatabase)
        {
            _logger = logger;
            _mongoDatabase = mongoDatabase;
        }

        [HttpGet("query-history")]
        public async Task<IActionResult> GetQueryHistory(
            [FromQuery] int limit = 50,
            [FromQuery] int offset = 0,
            [FromQuery] bool? starredOnly = null
        )
        {
            try
            {
                var collection = _mongoDatabase.GetCollection<QueryHistoryDocument>("query_history");
                var filter = BuildFilter(starredOnly);
                var sort = Builders<QueryHistoryDocument>.Sort.Descending(q => q.Timestamp);

                var history = await collection
                    .Find(filter)
                    .Sort(sort)
                    .Skip(offset)
                    .Limit(limit)
                    .ToListAsync();

                var totalCount = await collection.CountDocumentsAsync(filter);

                return Ok(new
                {
                    success = true,
                    data = history.Select(MapQueryToResponse),
                    total = totalCount,
                    limit = limit,
                    offset = offset
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve query history from MongoDB");
                return Problem($"Failed to retrieve query history: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("query-history/{id}")]
        public async Task<IActionResult> GetQueryById(string id)
        {
            try
            {
                var collection = _mongoDatabase.GetCollection<QueryHistoryDocument>("query_history");
                var filter = Builders<QueryHistoryDocument>.Filter.Eq(q => q.RequestId, id);
                var query = await collection.Find(filter).FirstOrDefaultAsync();

                return query != null
                    ? Ok(new
                    {
                        success = true,
                        data = MapQueryToDetailedResponse(query)
                    })
                    : NotFound(new { success = false, message = "Query not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve query {QueryId} from MongoDB", id);
                return Problem($"Failed to retrieve query: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("query-analytics")]
        public async Task<IActionResult> GetQueryAnalytics()
        {
            try
            {
                var collection = _mongoDatabase.GetCollection<QueryHistoryDocument>("query_history");

                var totalQueries = await collection.CountDocumentsAsync(FilterDefinition<QueryHistoryDocument>.Empty);
                var successfulQueries = await collection.CountDocumentsAsync(Builders<QueryHistoryDocument>.Filter.Eq(q => q.Success, true));
                var avgExecutionTime = await CalculateAverageExecutionTimeAsync(collection, successfulQueries, totalQueries);
                var topPrompts = await GetTopPromptsAsync(collection);
                var queriesByDay = await GetQueriesByDayAsync(collection);

                return Ok(new
                {
                    success = true,
                    analytics = new
                    {
                        totalQueries = (int)totalQueries,
                        successfulQueries = (int)successfulQueries,
                        successRate = totalQueries > 0 ? (double)successfulQueries / totalQueries * 100 : 0,
                        avgExecutionTime = avgExecutionTime,
                        topPrompts = topPrompts,
                        queriesByDay = queriesByDay
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve analytics from MongoDB");
                return Problem($"Failed to retrieve analytics: {ex.Message}", statusCode: 500);
            }
        }

        [HttpPost("toggle-star")]
        public async Task<IActionResult> ToggleStar([FromBody] ToggleStarRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.QueryId))
                {
                    return BadRequest(new { success = false, message = "QueryId is required" });
                }

                var collection = _mongoDatabase.GetCollection<QueryHistoryDocument>("query_history");
                var filter = Builders<QueryHistoryDocument>.Filter.Eq(q => q.RequestId, request.QueryId);
                var update = Builders<QueryHistoryDocument>.Update
                    .Set(q => q.Starred, request.IsStarred)
                    .Set(q => q.StarredAt, request.IsStarred ? DateTime.UtcNow : (DateTime?)null);

                var result = await collection.UpdateOneAsync(filter, update);

                if (result.MatchedCount == 0)
                {
                    return NotFound(new { success = false, message = "Query not found" });
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle star status");
                return Problem($"Failed to toggle star: {ex.Message}", statusCode: 500);
            }
        }

        [HttpPost("clear-all-stars")]
        public async Task<IActionResult> ClearAllStars()
        {
            try
            {
                var collection = _mongoDatabase.GetCollection<QueryHistoryDocument>("query_history");
                var filter = Builders<QueryHistoryDocument>.Filter.Eq(q => q.Starred, true);
                var update = Builders<QueryHistoryDocument>.Update
                    .Set(q => q.Starred, false)
                    .Set(q => q.StarredAt, null);

                var result = await collection.UpdateManyAsync(filter, update);

                return Ok(new { success = true, count = result.ModifiedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear all stars");
                return Problem($"Failed to clear all stars: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("error-logs")]
        public async Task<IActionResult> GetErrorLogs(
            [FromQuery] int limit = 50,
            [FromQuery] int offset = 0)
        {
            try
            {
                var collection = _mongoDatabase.GetCollection<QueryHistoryDocument>("query_history");
                var filter = Builders<QueryHistoryDocument>.Filter.Eq(q => q.Success, false);
                var sort = Builders<QueryHistoryDocument>.Sort.Descending(q => q.Timestamp);

                var errors = await collection
                    .Find(filter)
                    .Sort(sort)
                    .Skip(offset)
                    .Limit(limit)
                    .ToListAsync();

                var totalCount = await collection.CountDocumentsAsync(filter);

                return Ok(new
                {
                    success = true,
                    data = errors.Select(MapQueryToResponse),
                    total = totalCount,
                    limit = limit,
                    offset = offset
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve error logs from MongoDB");
                return Problem($"Failed to retrieve error logs: {ex.Message}", statusCode: 500);
            }
        }

        #region Private Helper Methods

        private static FilterDefinition<QueryHistoryDocument> BuildFilter(bool? starredOnly)
        {
            return starredOnly == true
                ? Builders<QueryHistoryDocument>.Filter.Eq(q => q.Starred, true)
                : Builders<QueryHistoryDocument>.Filter.Empty;
        }

        private static object MapQueryToResponse(QueryHistoryDocument q)
        {
            return new
            {
                id = q.RequestId,
                prompt = q.Prompt,
                generated_sql = q.Sql,
                execution_time_ms = q.ExecutionTimeMs,
                row_count = q.RowCount,
                ai_generated = q.AiGenerated,
                model_used = q.Model,
                status = q.Success ? "success" : "error",
                error_message = string.IsNullOrEmpty(q.ErrorMessage) ? null : q.ErrorMessage,
                request_id = q.RequestId,
                user_identifier = q.UserIdentifier,
                created_at = q.Timestamp,
                executed_at = q.Timestamp,
                starred = q.Starred,
                starredAt = q.StarredAt
            };
        }

        private static object MapQueryToDetailedResponse(QueryHistoryDocument query)
        {
            return new
            {
                id = query.RequestId,
                prompt = query.Prompt,
                generated_sql = query.Sql,
                execution_time_ms = query.ExecutionTimeMs,
                row_count = query.RowCount,
                ai_generated = query.AiGenerated,
                model_used = query.Model,
                status = query.Success ? "success" : "error",
                error_message = string.IsNullOrEmpty(query.ErrorMessage) ? null : query.ErrorMessage,
                request_id = query.RequestId,
                user_identifier = query.UserIdentifier,
                ip_address = query.IpAddress,
                user_agent = query.UserAgent,
                created_at = query.Timestamp,
                executed_at = query.Timestamp
            };
        }

        private static async Task<double> CalculateAverageExecutionTimeAsync(
            IMongoCollection<QueryHistoryDocument> collection,
            long successfulQueries,
            long totalQueries)
        {
            if (totalQueries == 0) return 0;

            var successfulFilter = Builders<QueryHistoryDocument>.Filter.Eq(q => q.Success, true);
            var executionTimes = await collection
                .Find(successfulFilter)
                .Project(Builders<QueryHistoryDocument>.Projection.Expression(q => q.ExecutionTimeMs))
                .ToListAsync();

            return executionTimes.Any() ? executionTimes.Average() : 0;
        }

        private static async Task<List<object>> GetTopPromptsAsync(IMongoCollection<QueryHistoryDocument> collection)
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("success", true)),
                new BsonDocument("$group", new BsonDocument {
                    { "_id", "$prompt" },
                    { "count", new BsonDocument("$sum", 1) }
                }),
                new BsonDocument("$sort", new BsonDocument("count", -1)),
                new BsonDocument("$limit", 10)
            };

            var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(p => new
            {
                prompt = p["_id"].AsString,
                count = p["count"].AsInt32
            }).ToList<object>();
        }

        private static async Task<List<object>> GetQueriesByDayAsync(IMongoCollection<QueryHistoryDocument> collection)
        {
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument {
                    { "_id", new BsonDocument("$dateToString", new BsonDocument {
                        { "format", "%Y-%m-%d" },
                        { "date", "$timestamp" }
                    })},
                    { "count", new BsonDocument("$sum", 1) }
                }),
                new BsonDocument("$sort", new BsonDocument("_id", -1)),
                new BsonDocument("$limit", 30)
            };

            var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(q => new
            {
                date = q["_id"].AsString,
                count = q["count"].AsInt32
            }).ToList<object>();
        }

        #endregion
    }

    public class ToggleStarRequest
    {
        public string QueryId { get; set; } = string.Empty;
        public bool IsStarred { get; set; }
    }
}
