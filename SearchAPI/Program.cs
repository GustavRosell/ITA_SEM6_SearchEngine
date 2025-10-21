using SearchAPI.Data;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// Read database path from environment variable or use default
var databasePath = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? Paths.DATABASE;
Console.WriteLine($"[SearchAPI] Using database: {databasePath}");

// Register DatabaseSqlite as singleton with configured path
builder.Services.AddSingleton<IDatabase>(sp => new DatabaseSqlite(databasePath));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTPS redirection removed for local development and load balancer compatibility

app.MapControllers();

app.Run();
