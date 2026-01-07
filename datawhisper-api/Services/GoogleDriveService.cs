using System.Data;
using System.Text;
using Dapper;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using DataWhisper.API.Configuration;
using DataWhisper.API.Models;
using Npgsql;

namespace DataWhisper.API.Services
{
    /// <summary>
    /// Service for Google Drive API operations including OAuth, file search, and content download
    /// </summary>
    public class GoogleDriveService
    {
        private readonly GoogleDriveConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<GoogleDriveService> _logger;

        public GoogleDriveService(
            IOptions<GoogleDriveConfiguration> config,
            IConfiguration configuration,
            ILogger<GoogleDriveService> logger)
        {
            _config = config.Value;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            _logger = logger;
        }

        /// <summary>
        /// Generate OAuth authorization URL for Google Drive consent screen
        /// </summary>
        public string GetAuthorizationUrl()
        {
            try
            {
                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _config.ClientId,
                        ClientSecret = _config.ClientSecret
                    },
                    Scopes = _config.Scopes,
                    DataStore = null
                });

                var state = Guid.NewGuid().ToString();
                var uri = flow.CreateAuthorizationUrlRequest(_config.RedirectUri)
                    .WithState(state)
                    .WithAccessType("offline")
                    .WithApprovalPrompt("force")
                    .Build();

                _logger.LogInformation("Generated Google Drive OAuth authorization URL");
                return uri;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Google Drive OAuth URL");
                throw;
            }
        }

        /// <summary>
        /// Exchange OAuth authorization code for access and refresh tokens
        /// </summary>
        public async Task<bool> ExchangeCodeForTokenAsync(string code)
        {
            try
            {
                _logger.LogInformation("Exchanging Google Drive authorization code for tokens");

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _config.ClientId,
                        ClientSecret = _config.ClientSecret
                    },
                    Scopes = _config.Scopes,
                    DataStore = null
                });

                var tokenResponse = await flow.ExchangeCodeForTokenAsync("", code, _config.RedirectUri, CancellationToken.None);

                // Calculate expiry time (default 1 hour from now)
                var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600);

                // Store tokens in database
                using var db = new NpgsqlConnection(_connectionString);
                await db.OpenAsync();

                // Deactivate old tokens
                await db.ExecuteAsync(
                    "UPDATE google_drive_tokens SET is_active = FALSE WHERE is_active = TRUE");

                // Insert new tokens
                var sql = @"
                    INSERT INTO google_drive_tokens
                    (access_token, refresh_token, token_type, expires_at, scope, is_active)
                    VALUES (@AccessToken, @RefreshToken, @TokenType, @ExpiresAt, @Scope, TRUE)
                    RETURNING id";

                var tokenId = await db.QuerySingleAsync<int>(sql, new
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    TokenType = tokenResponse.TokenType,
                    ExpiresAt = expiresAt,
                    Scope = string.Join(" ", _config.Scopes)
                });

                _logger.LogInformation("Google Drive tokens stored successfully. Token ID: {TokenId}", tokenId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging Google Drive authorization code");
                return false;
            }
        }

        /// <summary>
        /// Get active Google Drive connection status
        /// </summary>
        public async Task<GoogleDriveStatus> GetConnectionStatusAsync()
        {
            try
            {
                using var db = new NpgsqlConnection(_connectionString);
                await db.OpenAsync();

                var sql = @"
                    SELECT id, access_token, refresh_token, expires_at, created_at, is_active
                    FROM google_drive_tokens
                    WHERE is_active = TRUE
                    ORDER BY created_at DESC
                    LIMIT 1";

                var token = await db.QueryFirstOrDefaultAsync<GoogleDriveToken>(sql);

                if (token == null)
                {
                    return new GoogleDriveStatus
                    {
                        IsConnected = false,
                        ConnectedAt = null
                    };
                }

                // Check if token is expired
                if (token.ExpiresAt <= DateTime.UtcNow.AddMinutes(5))
                {
                    _logger.LogWarning("Google Drive token is expired. IsConnected will be false.");
                    return new GoogleDriveStatus
                    {
                        IsConnected = false,
                        ConnectedAt = null
                    };
                }

                return new GoogleDriveStatus
                {
                    IsConnected = true,
                    ConnectedAt = token.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Google Drive connection status");
                return new GoogleDriveStatus
                {
                    IsConnected = false,
                    ConnectedAt = null
                };
            }
        }

        /// <summary>
        /// Get active Google Drive tokens from database
        /// </summary>
        private async Task<GoogleDriveToken?> GetActiveTokenAsync()
        {
            try
            {
                using var db = new NpgsqlConnection(_connectionString);
                await db.OpenAsync();

                var sql = @"
                    SELECT id, access_token, refresh_token, token_type, expires_at, scope, created_at, updated_at, is_active
                    FROM google_drive_tokens
                    WHERE is_active = TRUE
                    ORDER BY created_at DESC
                    LIMIT 1";

                return await db.QueryFirstOrDefaultAsync<GoogleDriveToken>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Google Drive tokens from database");
                return null;
            }
        }

        /// <summary>
        /// Create authenticated Drive service using stored tokens
        /// </summary>
        private async Task<DriveService?> CreateDriveServiceAsync()
        {
            try
            {
                var token = await GetActiveTokenAsync();
                if (token == null)
                {
                    _logger.LogWarning("No active Google Drive token found");
                    return null;
                }

                // Check if token needs refresh
                if (token.ExpiresAt <= DateTime.UtcNow.AddMinutes(5))
                {
                    _logger.LogInformation("Token expired or expiring soon, attempting refresh");
                    var refreshed = await RefreshTokenAsync(token);
                    if (!refreshed)
                    {
                        return null;
                    }
                    // Reload token after refresh
                    token = await GetActiveTokenAsync();
                    if (token == null)
                    {
                        return null;
                    }
                }

                // Create credential using the access token
                var credential = GoogleCredential.FromAccessToken(token.AccessToken)
                    .CreateScoped(_config.Scopes);

                var service = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "DataWhisper"
                });

                return service;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Drive service");
                return null;
            }
        }

        /// <summary>
        /// Refresh expired access token using refresh token
        /// </summary>
        private async Task<bool> RefreshTokenAsync(GoogleDriveToken oldToken)
        {
            try
            {
                _logger.LogInformation("Refreshing Google Drive access token");

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _config.ClientId,
                        ClientSecret = _config.ClientSecret
                    }
                });

                var tokenResponse = new TokenResponse
                {
                    RefreshToken = oldToken.RefreshToken
                };

                await flow.RefreshTokenAsync("", tokenResponse, CancellationToken.None);

                var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600);

                // Update tokens in database
                using var db = new NpgsqlConnection(_connectionString);
                await db.OpenAsync();

                var sql = @"
                    UPDATE google_drive_tokens
                    SET access_token = @AccessToken,
                        expires_at = @ExpiresAt,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE id = @Id";

                var rows = await db.ExecuteAsync(sql, new
                {
                    AccessToken = tokenResponse.AccessToken,
                    ExpiresAt = expiresAt,
                    Id = oldToken.Id
                });

                _logger.LogInformation("Google Drive token refreshed successfully. Rows updated: {Rows}", rows);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing Google Drive token");
                return false;
            }
        }

        /// <summary>
        /// Search Google Drive for files matching the query
        /// </summary>
        public async Task<List<GoogleDriveFile>> SearchFilesAsync(string query, int maxResults = 3)
        {
            try
            {
                _logger.LogInformation("Searching Google Drive for: {Query}", query);

                var service = await CreateDriveServiceAsync();
                if (service == null)
                {
                    _logger.LogWarning("Cannot search Drive: Not authenticated");
                    return new List<GoogleDriveFile>();
                }

                var searchQuery = $"fullText contains '{query.Replace("'", "\\'")}'";

                var listRequest = service.Files.List();
                listRequest.Q = searchQuery;
                listRequest.PageSize = maxResults;
                listRequest.Fields = "files(id, name, mimeType, webViewLink, size)";

                var result = await listRequest.ExecuteAsync();

                var files = result.Files?.Select(f => new GoogleDriveFile
                {
                    FileId = f.Id,
                    Title = f.Name,
                    MimeType = f.MimeType,
                    WebViewLink = f.WebViewLink,
                    Size = f.Size ?? 0
                }).ToList() ?? new List<GoogleDriveFile>();

                _logger.LogInformation("Found {Count} files matching query: {Query}", files.Count, query);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Google Drive");
                return new List<GoogleDriveFile>();
            }
        }

        /// <summary>
        /// Download or export file content from Google Drive
        /// </summary>
        public async Task<string?> DownloadFileContentAsync(string fileId, string mimeType)
        {
            try
            {
                _logger.LogInformation("Downloading file content for FileId: {FileId}, MimeType: {MimeType}", fileId, mimeType);

                var service = await CreateDriveServiceAsync();
                if (service == null)
                {
                    _logger.LogWarning("Cannot download file: Not authenticated");
                    return null;
                }

                // Get file metadata to check size
                var fileRequest = service.Files.Get(fileId);
                fileRequest.Fields = "size";
                var fileMetadata = await fileRequest.ExecuteAsync();

                if (fileMetadata.Size.HasValue && fileMetadata.Size.Value > _config.MaxFileSize)
                {
                    _logger.LogWarning("File size {Size} exceeds limit {MaxSize}",
                        fileMetadata.Size.Value, _config.MaxFileSize);
                    return null;
                }

                string content;

                // Handle Google Docs (need to export)
                if (mimeType == "application/vnd.google-apps.document")
                {
                    var exportRequest = service.Files.Export(fileId, "text/plain");
                    using var stream = new MemoryStream();
                    await exportRequest.DownloadAsync(stream);
                    content = Encoding.UTF8.GetString(stream.ToArray());
                }
                // Handle Google Slides (export to text)
                else if (mimeType == "application/vnd.google-apps.presentation")
                {
                    var exportRequest = service.Files.Export(fileId, "text/plain");
                    using var stream = new MemoryStream();
                    await exportRequest.DownloadAsync(stream);
                    content = Encoding.UTF8.GetString(stream.ToArray());
                }
                // Handle Google Sheets (export to text/csv)
                else if (mimeType == "application/vnd.google-apps.spreadsheet")
                {
                    var exportRequest = service.Files.Export(fileId, "text/csv");
                    using var stream = new MemoryStream();
                    await exportRequest.DownloadAsync(stream);
                    content = Encoding.UTF8.GetString(stream.ToArray());
                }
                // Handle regular files (PDF, DOCX, TXT, etc.)
                else
                {
                    var downloadRequest = service.Files.Get(fileId);
                    using var stream = new MemoryStream();
                    await downloadRequest.DownloadAsync(stream);
                    content = Encoding.UTF8.GetString(stream.ToArray());
                }

                _logger.LogInformation("Successfully downloaded file content. Length: {Length}", content.Length);
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file content for FileId: {FileId}", fileId);
                return null;
            }
        }
    }
}
