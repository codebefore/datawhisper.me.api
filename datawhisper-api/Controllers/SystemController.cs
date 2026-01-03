using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using DataWhisper.API;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.RateLimiting;

namespace DataWhisper.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("SystemPolicy")]
    public class SystemController : ControllerBase
    {
        private readonly ILogger<SystemController> _logger;
        private readonly string _connectionString;
        private readonly AIServiceClient _aiServiceClient;

        public SystemController(
            ILogger<SystemController> logger,
            IConfiguration configuration,
            AIServiceClient aiServiceClient)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _aiServiceClient = aiServiceClient;
        }

        [HttpGet("ai-status")]
        public async Task<IActionResult> GetAiStatus()
        {
            var status = await _aiServiceClient.GetStatusAsync();
            return status != null
                ? Ok(status)
                : Problem("AI service unavailable", statusCode: 503);
        }

        [HttpGet("test")]
        public IActionResult GetTestMessage()
        {
            var messages = new Dictionary<string, string>
            {
                ["en"] = "DataWhisper API is running!",
                ["tr"] = "DataWhisper API çalışıyor!"
            };

            return Ok(new {
                Message = GetMessage(messages, HttpContext),
                Timestamp = DateTime.UtcNow,
                Status = "Ready",
                AIIntegration = "DataWhisper Engine enabled"
            });
        }

        [HttpGet("messages-test")]
        public IActionResult GetMessagesTest()
        {
            var messages = new Dictionary<string, string>
            {
                ["en"] = "Query executed successfully",
                ["tr"] = "Sorgu başarıyla çalıştırıldı"
            };

            return Ok(new {
                English = messages["en"],
                Turkish = messages["tr"],
                CurrentLanguage = GetMessage(messages, HttpContext),
                AvailableLanguages = new[] { "en", "tr" }
            });
        }

        [HttpGet("db-config")]
        public IActionResult GetDatabaseConfig()
        {
            return Ok(new {
                ConnectionString = _connectionString,
                EnvironmentVariables = new {
                    DefaultConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                }
            });
        }

        [HttpGet("db-test")]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            using var db = new NpgsqlConnection(_connectionString);
            await db.OpenAsync();
            var result = await db.ExecuteScalarAsync("SELECT 'Database connected!' as status");
            return Ok(new { Success = true, Message = result, Timestamp = DateTime.UtcNow });
        }

        [HttpGet]
        public IActionResult GetRoot()
        {
            return Ok("DataWhisper API is running!");
        }

        #region Private Helper Methods

        private string GetMessage(Dictionary<string, string> messages, HttpContext? context = null)
        {
            var language = "en"; // Default language

            if (context != null)
            {
                // Try to get language from query parameter first
                var langParam = context.Request.Query["lang"].FirstOrDefault();
                if (!string.IsNullOrEmpty(langParam) && messages.ContainsKey(langParam))
                {
                    language = langParam;
                }
                else
                {
                    // Try to get from Accept-Language header
                    var acceptLanguage = context.Request.Headers["Accept-Language"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(acceptLanguage))
                    {
                        // Simple parsing: "tr-TR,tr;q=0.9,en;q=0.8" -> "tr"
                        var primaryLang = acceptLanguage.Split(',')[0].Split('-')[0].ToLower();
                        if (messages.ContainsKey(primaryLang))
                        {
                            language = primaryLang;
                        }
                    }
                }
            }

            return messages.GetValueOrDefault(language, messages["en"]);
        }

        #endregion
    }
}