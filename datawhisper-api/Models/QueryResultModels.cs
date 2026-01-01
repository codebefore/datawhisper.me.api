namespace DataWhisper.API.Models
{
    public class SqlGenerationResult
    {
        public bool IsSuccess { get; set; }
        public string? Sql { get; set; }
        public bool IsAIGenerated { get; set; }
        public AIGenerateSqlResponse? AiResponse { get; set; }
        public int? TotalRows { get; set; }
        public int? OpenAiLimit { get; set; }
        public object? ErrorResponse { get; set; }
    }

    public class PaginationResult
    {
        public string OriginalSql { get; set; } = string.Empty;
        public string PaginatedSql { get; set; } = string.Empty;
        public int? OpenAiLimit { get; set; }
    }

    public class QueryExecutionResult
    {
        public List<Dictionary<string, object>> Data { get; set; } = new();
        public int TotalRows { get; set; }
    }
}
