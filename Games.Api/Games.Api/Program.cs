using Amazon.EventBridge;
using Games.Api.Infrastructure.Events;
using Games.Api.Infrastructure.Persistence;
using Games.Api.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Nest;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// =======================
// SERVICES
// =======================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext (APENAS UMA VEZ)
builder.Services.AddDbContext<GamesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
);

// Event Sourcing
builder.Services.AddScoped<EventStore>();

// Elasticsearch
var elasticUri = builder.Configuration["Elastic:Uri"] ?? "http://localhost:9200";

var settings = new ConnectionSettings(new Uri(elasticUri))
    .DefaultIndex("games");

var client = new ElasticClient(settings);

builder.Services.AddSingleton<IElasticClient>(client);
builder.Services.AddScoped<IGameSearchService, GameSearchService>();

builder.Services.AddAWSService<IAmazonEventBridge>();
builder.Services.AddScoped<EventBridgePublisher>();

// =======================
// BUILD
// =======================

var app = builder.Build();

// =======================
// MIDDLEWARE
// =======================


if (Debugger.IsAttached)
{
    // Swagger SEM restrição de ambiente (ECS precisa disso)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Games API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/games/swagger/v1/swagger.json", "FCG Games API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Health check para ALB / ECS
app.MapGet("/health", () => Results.Ok("Healthy"));

// ESSENCIAL para Docker / ECS
app.Urls.Add("http://0.0.0.0:80");

app.Run();
