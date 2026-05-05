using AarhusSpaceProgram.Api.Data;
using AarhusSpaceProgram.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

try
{
    var loggerConfiguration = new LoggerConfiguration()
        .WriteTo.Console();

    var serilogMongoDbUrl = builder.Configuration["Serilog:MongoDbUrl"];
    var serilogMongoDbCollection = builder.Configuration["Serilog:MongoDbCollection"];

    var useMongoSink = false;
    if (!string.IsNullOrWhiteSpace(serilogMongoDbUrl) &&
        !string.IsNullOrWhiteSpace(serilogMongoDbCollection))
    {
        useMongoSink = true;
        var separator = serilogMongoDbUrl.Contains('?') ? "&" : "?";
        loggerConfiguration.WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(e =>
                e.Properties.ContainsKey("RequestMethod") &&
                e.Properties.ContainsKey("RequestPath") &&
                e.Properties.ContainsKey("StatusCode"))
            .WriteTo.MongoDBBson(
                databaseUrl: $"{serilogMongoDbUrl}{separator}connectTimeoutMS=2000&serverSelectionTimeoutMS=2000",
                collectionName: serilogMongoDbCollection,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information));
    }

    Log.Logger = loggerConfiguration.CreateLogger();

    Log.Information(useMongoSink
        ? "Serilog configured with MongoDB sink"
        : "Serilog configured with console sink");
}
catch (Exception ex)
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger();

    Log.Warning(ex, "Failed to configure MongoDB logging. Using console logging only.");
}

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v1");
builder.Services.AddHealthChecks();

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnection))
{
    throw new InvalidOperationException(
        "Missing ConnectionStrings:DefaultConnection. Set it via Docker Compose, environment variables, or user secrets.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        defaultConnection,
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddScoped<IMissionService, MissionService>();

var app = builder.Build();

app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Aarhus Space Program API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode}";

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path);
        diagnosticContext.Set("StatusCode", httpContext.Response.StatusCode);
        diagnosticContext.Set("Timestamp", DateTime.UtcNow);
    };

    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        var method = httpContext.Request.Method;
        if (method == "POST" || method == "PUT" || method == "DELETE")
        {
            return Serilog.Events.LogEventLevel.Information;
        }

        return Serilog.Events.LogEventLevel.Verbose;
    };
});

if (!string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    "true",
    StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var executionStrategy = context.Database.CreateExecutionStrategy();
    executionStrategy.Execute(() =>
    {
        context.Database.Migrate();
        DbInitializer.Initialize(context);
    });
}

app.Run();
