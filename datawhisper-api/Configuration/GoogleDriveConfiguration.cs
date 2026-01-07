namespace DataWhisper.API.Configuration
{
    /// <summary>
    /// Configuration settings for Google Drive integration
    /// </summary>
    public class GoogleDriveConfiguration
    {
        public const string SectionName = "GoogleDrive";

        /// <summary>
        /// OAuth 2.0 Client ID from Google Cloud Console
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// OAuth 2.0 Client Secret from Google Cloud Console
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Redirect URI for OAuth callback
        /// </summary>
        public string RedirectUri { get; set; } = string.Empty;

        /// <summary>
        /// Maximum file size to download (default: 20MB)
        /// </summary>
        public long MaxFileSize { get; set; } = 20971520; // 20MB

        /// <summary>
        /// Maximum number of files to process (default: 3)
        /// </summary>
        public int MaxFiles { get; set; } = 3;

        /// <summary>
        /// OAuth scopes for Google Drive API
        /// </summary>
        public string[] Scopes { get; set; } = new[]
        {
            "https://www.googleapis.com/auth/drive.readonly"
        };
    }
}
