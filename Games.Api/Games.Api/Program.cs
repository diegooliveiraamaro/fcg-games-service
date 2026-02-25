using Amazon.EventBridge;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Lambda;
using Games.Api.Infrastructure.Events;
using Games.Api.Infrastructure.Persistence;
using Games.Api.Infrastructure.Search;
using Games.Api.Messaging;
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

// DbContext
builder.Services.AddDbContext<GamesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
);

builder.Services.AddSingleton<RabbitPublisher>();
// Event Sourcing
builder.Services.AddScoped<EventStore>();

// =======================
// ELASTICSEARCH
// =======================



var elasticUri = string.IsNullOrWhiteSpace(builder.Configuration["Elastic:Uri"])
    ? "http://localhost:9200"
    : builder.Configuration["Elastic:Uri"];

var settings = new ConnectionSettings(new Uri(elasticUri))
    .DefaultIndex("games")
    .DisableDirectStreaming()
    .PrettyJson()
    .OnRequestCompleted(details =>
    {
        Debug.WriteLine(details.DebugInformation);
    });

var client = new ElasticClient(settings);

builder.Services.AddSingleton<IElasticClient>(client);
builder.Services.AddScoped<IGameSearchService, GameSearchService>();


// =======================
// BUILD
// =======================

var app = builder.Build();



// =======================
// MIDDLEWARE
// =======================

app.UsePathBase("/games");
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    // c.SwaggerEndpoint("/swagger/v1/swagger.json", "Games API v1");
    c.SwaggerEndpoint("/games/swagger/v1/swagger.json", "Games API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok("Healthy"));

// Docker / ECS
app.Urls.Add("http://0.0.0.0:80");

// =======================
// GARANTE ÍNDICE ELASTIC
// =======================

using (var scope = app.Services.CreateScope())
{
    var elastic = scope.ServiceProvider.GetRequiredService<IElasticClient>();
    
    try
    {
        await GameSearchService.EnsureElasticIndexAsync(client);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Elastic não disponível. Continuando sem busca.");
    }
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
    db.Database.Migrate();
}

app.Run();