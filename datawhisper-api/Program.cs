using DataWhisper.API;
using DataWhisper.API.Models;
using MongoDB.Driver;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "DataWhisper API", Version = "v1" });
    // Add XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// CORS for frontend
var corsAllowedOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"]
    ?? "http://localhost:3000;http://localhost:3001;http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins(corsAllowedOrigins.Split(';', StringSplitOptions.RemoveEmptyEntries))
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Add MongoDB client
var mongoSettings = MongoClientSettings.FromConnectionString(
    builder.Configuration.GetConnectionString("MongoDbConnection")
    ?? "mongodb://datawhisper_user:datawhisper_mongo_pass@localhost:27017/datawhisper_analytics?authSource=admin");
mongoSettings.SocketTimeout = TimeSpan.FromSeconds(5);
var mongoClient = new MongoClient(mongoSettings);
builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddScoped(sp => {
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("datawhisper_analytics");
});

// Add AI Service Client
builder.Services.AddHttpClient<AIServiceClient>(client =>
{
    var aiServiceUrl = Environment.GetEnvironmentVariable("AI_SERVICE_URL") ?? "http://datawhisper-ai:5003";
    client.BaseAddress = new Uri(aiServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add SQL Fixer Service
builder.Services.AddScoped<SqlFixerService>();

// Add Logging
builder.Services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Add Forwarded Headers for rate limiting
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Global policy - applied to all endpoints unless overridden
    options.AddPolicy("GlobalPolicy", context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_GLOBAL") ?? "100"),
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // Query Controller - Strict limit (AI is expensive)
    options.AddPolicy("QueryPolicy", context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_QUERY") ?? "30"),
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // Analytics Controller - Medium limit (DB intensive)
    options.AddPolicy("AnalyticsPolicy", context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_ANALYTICS") ?? "60"),
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // System Controller - Lenient limit (health checks)
    options.AddPolicy("SystemPolicy", context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_SYSTEM") ?? "200"),
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // Configure global rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            message = "Rate limit exceeded. Please try again later.",
            retryAfter = TimeSpan.FromSeconds(60).TotalSeconds,
            timestamp = DateTime.UtcNow
        };

        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };

    // Global limiter - fallback for endpoints without specific policy
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_GLOBAL_FALLBACK") ?? "150"),
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});


var app = builder.Build();

// Global Exception Handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        logger.LogError(exception, "💥 Unhandled exception: {Message}", exception?.Message);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            message = "An unexpected error occurred",
            error = exception?.Message,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(response);
    });
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DataWhisper API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the root
    });
}

app.UseHttpsRedirection();

// Use Forwarded Headers for rate limiting
app.UseForwardedHeaders();

// Use Rate Limiting
app.UseRateLimiter();

// Use CORS
app.UseCors("AllowFrontend");

// Map Controllers - All endpoints handled by controllers
app.MapControllers();

// Health check endpoint
app.MapGet("/", () => "DataWhisper API is running!");

app.Run();