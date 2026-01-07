using DataWhisper.API.Models;

namespace DataWhisper.API.Services
{
    /// <summary>
    /// Service for orchestrating document Q&A operations
    /// Integrates Google Drive search, file download, and AI-powered question answering
    /// </summary>
    public class DocumentQAService
    {
        private readonly GoogleDriveService _googleDriveService;
        private readonly AIServiceClient _aiServiceClient;
        private readonly ILogger<DocumentQAService> _logger;

        public DocumentQAService(
            GoogleDriveService googleDriveService,
            AIServiceClient aiServiceClient,
            ILogger<DocumentQAService> logger)
        {
            _googleDriveService = googleDriveService;
            _aiServiceClient = aiServiceClient;
            _logger = logger;
        }

        /// <summary>
        /// Answer a question by searching Google Drive documents and using AI
        /// </summary>
        public async Task<DocumentQueryResponse> AnswerDocumentQuestionAsync(
            string question,
            string? language = "en")
        {
            try
            {
                _logger.LogInformation("Processing document Q&A request. Question: {Question}", question);

                // Step 1: Extract keywords from question
                var keywords = ExtractKeywords(question);
                _logger.LogInformation("Extracted keywords: {Keywords}", string.Join(", ", keywords));

                // Step 2: Search Google Drive for relevant files
                var searchQuery = string.Join(" OR ", keywords);
                var files = await _googleDriveService.SearchFilesAsync(searchQuery, maxResults: 3);

                if (files.Count == 0)
                {
                    _logger.LogWarning("No files found for keywords: {Keywords}", searchQuery);
                    return new DocumentQueryResponse
                    {
                        ModeUsed = "doc",
                        Success = false,
                        Message = "No relevant documents found in Google Drive",
                        Timestamp = DateTime.UtcNow
                    };
                }

                _logger.LogInformation("Found {FileCount} files", files.Count);

                // Step 3: Download file contents
                var contents = new List<string>();
                var downloadedFiles = new List<GoogleDriveFile>();

                foreach (var file in files)
                {
                    var content = await _googleDriveService.DownloadFileContentAsync(file.FileId, file.MimeType);

                    if (!string.IsNullOrEmpty(content))
                    {
                        contents.Add(content);
                        downloadedFiles.Add(file);
                        _logger.LogInformation("Downloaded file: {FileName}, Content length: {Length}",
                            file.Title, content.Length);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to download or empty content for file: {FileName}", file.Title);
                    }

                    // Respect max files limit
                    if (downloadedFiles.Count >= 3)
                    {
                        break;
                    }
                }

                if (contents.Count == 0)
                {
                    _logger.LogWarning("No file contents could be downloaded");
                    return new DocumentQueryResponse
                    {
                        ModeUsed = "doc",
                        Success = false,
                        Message = "Could not download file contents",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Step 4: Call AI service for document Q&A
                _logger.LogInformation("Calling AI service with {DocCount} documents", contents.Count);
                var aiResponse = await _aiServiceClient.AnswerDocumentQuestionAsync(
                    question,
                    contents.ToArray(),
                    language ?? "en");

                if (aiResponse == null || !aiResponse.Success)
                {
                    _logger.LogWarning("AI service failed to process documents");
                    return new DocumentQueryResponse
                    {
                        ModeUsed = "doc",
                        Success = false,
                        Message = "Failed to analyze documents with AI",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Step 5: Build response
                var topDocuments = downloadedFiles.Select((f, index) => new GoogleDriveDocument
                {
                    Title = f.Title,
                    WebViewLink = f.WebViewLink,
                    Snippet = aiResponse.SourceSnippets.Length > index
                        ? aiResponse.SourceSnippets[index]
                        : ExtractSnippet(contents[index], 150)
                }).ToArray();

                _logger.LogInformation("Document Q&A completed successfully. Answer: {Answer}",
                    aiResponse answer?.Substring(0, Math.Min(100, aiResponse.Answer.Length)) ?? "N/A");

                return new DocumentQueryResponse
                {
                    ModeUsed = "doc",
                    Answer = aiResponse.Answer,
                    SummaryBullets = aiResponse.SummaryBullets,
                    TopDocuments = topDocuments,
                    Success = true,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document Q&A request");
                return new DocumentQueryResponse
                {
                    ModeUsed = "doc",
                    Success = false,
                    Message = "An error occurred while processing your request",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Check if Google Drive is connected
        /// </summary>
        public async Task<bool> IsConnectedAsync()
        {
            var status = await _googleDriveService.GetConnectionStatusAsync();
            return status.IsConnected;
        }

        /// <summary>
        /// Extract keywords from a question using simple NLP
        /// </summary>
        private string[] ExtractKeywords(string question)
        {
            // Common stop words to ignore
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "what", "where", "when", "who", "why", "how", "is", "are", "was", "were",
                "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
                "of", "with", "by", "from", "about", "as", "into", "through", "during",
                "before", "after", "above", "below", "between", "under", "again", "further",
                "then", "once", "here", "there", "when", "where", "why", "how", "all",
                "any", "both", "each", "few", "more", "most", "other", "some", "such",
                "no", "nor", "not", "only", "own", "same", "so", "than", "too", "very",
                "can", "will", "just", "should", "now", "tell", "me", "give", "show",
                "explain", "describe", "list", "find", "search", "look", "need", "want"
            };

            // Split question into words and filter out stop words
            var words = question.Split(new[] { ' ', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']', '{', '}' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var keywords = words
                .Where(w => w.Length > 2 && !stopWords.Contains(w))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5) // Limit to 5 keywords
                .ToArray();

            // If no keywords found, use the original question split by spaces
            if (keywords.Length == 0)
            {
                return question.Split(' ')
                    .Where(w => w.Length > 0)
                    .Take(3)
                    .ToArray();
            }

            return keywords;
        }

        /// <summary>
        /// Extract a snippet from text for preview
        /// </summary>
        private string ExtractSnippet(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            // Remove extra whitespace and newlines
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            if (text.Length <= maxLength)
            {
                return text;
            }

            // Try to find a sentence break
            var truncated = text.Substring(0, maxLength);
            var lastPeriod = truncated.LastIndexOf('.');
            var lastSpace = truncated.LastIndexOf(' ');

            if (lastPeriod > maxLength * 0.7)
            {
                return text.Substring(0, lastPeriod + 1) + "...";
            }
            else if (lastSpace > maxLength * 0.7)
            {
                return text.Substring(0, lastSpace) + "...";
            }

            return truncated + "...";
        }
    }
}
