namespace DataWhisper.API.Models
{
    /// <summary>
    /// Response model for document Q&A queries
    /// </summary>
    public record DocumentQueryResponse
    {
        public string ModeUsed { get; init; } = "doc";
        public string Answer { get; init; } = string.Empty;
        public string[] SummaryBullets { get; init; } = Array.Empty<string>();
        public GoogleDriveDocument[] TopDocuments { get; init; } = Array.Empty<GoogleDriveDocument>();
        public bool Success { get; init; }
        public string? Message { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a Google Drive document in the response
    /// </summary>
    public record GoogleDriveDocument
    {
        public string Title { get; init; } = string.Empty;
        public string WebViewLink { get; init; } = string.Empty;
        public string Snippet { get; init; } = string.Empty;
    }

    /// <summary>
    /// Google Drive OAuth token storage model
    /// </summary>
    public record GoogleDriveToken
    {
        public int Id { get; init; }
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public string TokenType { get; init; } = "Bearer";
        public DateTime ExpiresAt { get; init; }
        public string? Scope { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public bool IsActive { get; init; }
    }

    /// <summary>
    /// Google Drive connection status
    /// </summary>
    public record GoogleDriveStatus
    {
        public bool IsConnected { get; init; }
        public DateTime? ConnectedAt { get; init; }
    }

    /// <summary>
    /// Google Drive file search result
    /// </summary>
    public record GoogleDriveFile
    {
        public string FileId { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string MimeType { get; init; } = string.Empty;
        public string WebViewLink { get; init; } = string.Empty;
        public long Size { get; init; }
    }

    /// <summary>
    /// Document Q&A request to AI service
    /// </summary>
    public record DocumentQARequest
    {
        public string Question { get; init; } = string.Empty;
        public string[] Documents { get; init; } = Array.Empty<string>();
        public string? Language { get; init; } = "en";
    }

    /// <summary>
    /// Document Q&A response from AI service
    /// </summary>
    public record DocumentQAResponse
    {
        public bool Success { get; init; }
        public string Answer { get; init; } = string.Empty;
        public string[] SummaryBullets { get; init; } = Array.Empty<string>();
        public string[] SourceSnippets { get; init; } = Array.Empty<string>();
    }
}
