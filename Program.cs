using FantasyNBA.ApiClients;
using FantasyNBA.Data;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Models.Config;
using FantasyNBA.Parsers;
using FantasyNBA.Services;
using FantasyNBA.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// -------------------------
// Configure Services
// -------------------------

// Add Controllers
builder.Services.AddControllers();

// Enable CORS (for Angular frontend)
builder.Services.AddCors();

// Register DbContext
builder.Services.AddDbContext<FantasyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register configuration settings
builder.Services.Configure<ApiProviderSettings>("BalldontlieApi", builder.Configuration.GetSection("BalldontlieApi"));
builder.Services.Configure<ApiProviderSettings>("NBA_API", builder.Configuration.GetSection("NBA_API"));
builder.Services.Configure<SyncSettings>("SyncSettings", builder.Configuration.GetSection("SyncSettings"));

// Register application services
builder.Services.AddScoped<IDbProvider, DbProvider>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IGameStatService, GameStatService>();
builder.Services.AddScoped<IGenericLogger, GenericLogger>();

// Register HttpClient and fetcher
builder.Services.AddHttpClient<IExternalApiDataFetcher, ExternalApiDataFetcher>();

// Register parsers
builder.Services.AddScoped<IApiParser, BallDontLiePlayerParser>();
builder.Services.AddScoped<IApiParser, NBA_APIParser>();

// Register API clients
builder.Services.AddScoped<INbaApiClient, BallDontLieApiClient>(sp =>
{
    var fetcher = sp.GetRequiredService<IExternalApiDataFetcher>();
    var parser = sp.GetServices<IApiParser>().OfType<BallDontLiePlayerParser>().First();
    var settings = sp.GetRequiredService<IOptionsMonitor<ApiProviderSettings>>().Get("BalldontlieApi");
    var loggerDb = sp.GetRequiredService<IGenericLogger>();

    return new BallDontLieApiClient(fetcher, parser, Options.Create(settings), settings.PageSize, loggerDb);
});

builder.Services.AddScoped<INbaApiClient, NbaApiClient>(sp =>
{
    var fetcher = sp.GetRequiredService<IExternalApiDataFetcher>();
    var parser = sp.GetServices<IApiParser>().OfType<NBA_APIParser>().First();
    var settings = sp.GetRequiredService<IOptionsMonitor<ApiProviderSettings>>().Get("NBA_API");
    var dbProvider = sp.GetRequiredService<IDbProvider>();
    var syncSettings = sp.GetRequiredService<IOptionsMonitor<SyncSettings>>().Get("SyncSettings");
    var genericLogger = sp.GetRequiredService<IGenericLogger>();
    var logger = sp.GetRequiredService<ILogger<INbaApiClient>>();

    return new NbaApiClient(fetcher, dbProvider, parser, Options.Create(settings), settings.PageSize, Options.Create(syncSettings), genericLogger, logger);
});

builder.Services.AddScoped<SyncService>();
builder.Services.AddSwaggerGen();

// -------------------------
// Build App & Middleware
// -------------------------

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();
