using FantasyNBA.ApiClients;
using FantasyNBA.Data;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Models.Config;
using FantasyNBA.Parsers;
using FantasyNBA.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// CORS (for Angular frontend)
builder.Services.AddCors();

// Register DbContext
builder.Services.AddDbContext<FantasyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register configuration settings
builder.Services.Configure<ApiProviderSettings>("BalldontlieApi", builder.Configuration.GetSection("BalldontlieApi"));
builder.Services.Configure<ApiProviderSettings>("NBA_API", builder.Configuration.GetSection("NBA_API"));

// Register services
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IGameStatService, GameStatService>();

// Register HttpClient and fetcher
builder.Services.AddHttpClient<IExternalApiDataFetcher, ExternalApiDataFetcher>();

// Register parser
builder.Services.AddScoped<IApiParser, BallDontLiePlayerParser>();
builder.Services.AddScoped<IApiParser, NBA_APIParser>();

// Register API clients
builder.Services.AddScoped<INbaApiClient, BallDontLieApiClient>(sp =>
{
    var fetcher = sp.GetRequiredService<IExternalApiDataFetcher>();
    var parser = sp.GetServices<IApiParser>().OfType<BallDontLiePlayerParser>().First();
    var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<ApiProviderSettings>>();
    var settings = optionsMonitor.Get("BalldontlieApi");
    return new BallDontLieApiClient(fetcher, parser, Options.Create(settings), settings.PageSize);
});

builder.Services.AddScoped<INbaApiClient, NbaApiClient>(sp =>
{
    var fetcher = sp.GetRequiredService<IExternalApiDataFetcher>();
    var parser = sp.GetServices<IApiParser>().OfType<NBA_APIParser>().First();
    var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<ApiProviderSettings>>();
    var settings = optionsMonitor.Get("NBA_API");
    return new NbaApiClient(fetcher, parser, Options.Create(settings), settings.PageSize);
});

builder.Services.AddScoped<SyncService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware
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
