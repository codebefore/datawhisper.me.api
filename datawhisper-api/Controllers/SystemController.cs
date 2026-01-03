using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using DataWhisper.API;
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
    }
}