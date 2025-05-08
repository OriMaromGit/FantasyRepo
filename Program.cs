using FantasyNBA.Data;
using FantasyNBA.Interfaces;
using FantasyNBA.Models.Config;
using FantasyNBA.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// CORS (for Angular frontend)
builder.Services.AddCors();

// Register DbContext
builder.Services.AddDbContext<FantasyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.Configure<ApiProviderSettings>(
    builder.Configuration.GetSection("BalldontlieApi"));

builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IGameStatService, GameStatService>();
builder.Services.AddHttpClient<INbaApiClient, NbaApiClient>();
builder.Services.AddScoped<PlayerSyncService>();

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
app.UseCors(); // allow API to be called from Angular
app.UseAuthorization();

app.MapControllers();
app.Run();
