using InvoiceProcessor.Api.Data;
using InvoiceProcessor.Api.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
Directory.CreateDirectory(logsPath);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(logsPath, "app-.log"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for Flutter
builder.Services.AddCors(options =>
{
    options.AddPolicy("FlutterPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Database - SQLite for cross-platform compatibility
var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InvoiceProcessor.db");
builder.Services.AddDbContext<InvoiceDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Custom Services
builder.Services.AddScoped<IOcrService, TesseractOcrService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IInvoiceParsingService, InvoiceParsingService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IFileProcessingService, FileProcessingService>();

var app = builder.Build();

// Database Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
    context.Database.EnsureCreated();
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FlutterPolicy");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Log.Information("Invoice Processor API started - Port: {Port}",
    app.Urls.FirstOrDefault() ?? "https://localhost:5001");

app.Run();