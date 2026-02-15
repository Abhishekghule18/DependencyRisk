using DependencyRisk.Core.Interfaces;
using DependencyRisk.Infrastructure.AI;
using DependencyRisk.Infrastructure.Data;
using DependencyRisk.Infrastructure.GitHub;
using DependencyRisk.Infrastructure.Parsers;
using DependencyRisk.Infrastructure.Scoring;
using DependencyRisk.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/dependencyrisk-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "DependencyRisk API", Version = "v1" });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

// CORS for Angular dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:80")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// File parsers (registered individually so IEnumerable<IFileParser> resolves all)
builder.Services.AddScoped<IFileParser, CsprojParser>();
builder.Services.AddScoped<IFileParser, PackageJsonParser>();
builder.Services.AddScoped<IFileParser, RequirementsTxtParser>();

// GitHub client
builder.Services.AddHttpClient<IGitHubClient, GitHubMetricsClient>();

// Scorer
builder.Services.AddScoped<IRiskScorer, WeightedRiskScorer>();

// AI summarizer
builder.Services.AddHttpClient<IAiSummarizer, OllamaRiskSummarizer>();

// Orchestrator
builder.Services.AddScoped<ScanOrchestrator>();

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
