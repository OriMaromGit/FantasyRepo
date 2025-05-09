using FantasyNBA.ApiClients;
using FantasyNBA.Data;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Models.Config;
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
builder.Services.Configure<ApiProviderSettings>(
    builder.Configuration.GetSection("BalldontlieApi"));

// Register services
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IGameStatService, GameStatService>();

// Register HttpClient and fetcher
builder.Services.AddHttpClient<IExternalApiDataFetcher, ExternalApiDataFetcher>();

// Register parser
builder.Services.AddScoped<IApiParser<Player>, BallDontLiePlayerParser>();

// Register NBA API client with page size manually provided
builder.Services.AddScoped<INbaApiClient>(sp =>
{
    var fetcher = sp.GetRequiredService<IExternalApiDataFetcher>();
    var parser = sp.GetRequiredService<IApiParser<Player>>();
    var settings = sp.GetRequiredService<IOptions<ApiProviderSettings>>();
    int pageSize = settings.Value.PageSize;

    return new BallDontLieApiClient(fetcher, parser, settings, pageSize);
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
