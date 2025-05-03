using FantasyNBA.Data;
using FantasyNBA.Interfaces;

using FantasyNBA.Services;

/*using FantasyNBA.Interfaces;
using FantasyNBA.Services;*/
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Register DbContext with SQL Server
builder.Services.AddDbContext<FantasyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services (DI)
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IGameStatService, GameStatService>();

// Add Swagger for API testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
