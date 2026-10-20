using GestorTurnos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using GestorTurnos.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<ExcelShiftImportService>();
builder.Services.AddScoped<ExcelShiftExportService>();

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<HolidayService>(client =>
{
    client.BaseAddress = new Uri("https://date.nager.at/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();


if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    const int maxAttempts = 15;
    var delay = TimeSpan.FromSeconds(3);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            Console.WriteLine("Database migrations applied successfully.");
            break;
        }
        catch (Microsoft.Data.SqlClient.SqlException)
            when (attempt < maxAttempts)
        {
            Console.WriteLine(
                $"SQL Server aún no disponible. Intento {attempt}/{maxAttempts}.");

            await Task.Delay(delay);
        }
    }
}

if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health");
app.Run();

public partial class Program { }
