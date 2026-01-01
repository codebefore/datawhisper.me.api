using DataWhisper.API;
using DataWhisper.API.Models;
using MongoDB.Driver;


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
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:5173")
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

// Use CORS
app.UseCors("AllowFrontend");

// Map Controllers - All endpoints handled by controllers
app.MapControllers();

// Health check endpoint
app.MapGet("/", () => "DataWhisper API is running!");

app.Run();