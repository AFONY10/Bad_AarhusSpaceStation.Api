using AarhusSpaceProgram.Api.Data;
using AarhusSpaceProgram.Api.Models;
using AarhusSpaceProgram.Api.Security;
using AarhusSpaceProgram.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using System.Text;

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
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer token from POST /api/auth/login."
        };

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, _) =>
    {
        var endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;
        var allowsAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();
        var requiresAuthorization = endpointMetadata.OfType<IAuthorizeData>().Any();

        if (allowsAnonymous || !requiresAuthorization)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, context.Document, null)] = []
        });

        return Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Missing Jwt:Key. Set it via Docker Compose, environment variables, or user secrets.");
}

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes for HMAC-SHA256 signing.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
}

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

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicyNames.ReadAccess, policy =>
        policy.RequireRole(RoleNames.Astronaut, RoleNames.Scientist, RoleNames.Manager));

    options.AddPolicy(AuthorizationPolicyNames.ManagerOnly, policy =>
        policy.RequireRole(RoleNames.Manager));

    options.AddPolicy(AuthorizationPolicyNames.ExperimentWriteAccess, policy =>
        policy.RequireRole(RoleNames.Scientist, RoleNames.Manager));
});

builder.Services.AddScoped<IMissionService, MissionService>();
builder.Services.AddScoped<IMissionLogRepository, MongoMissionLogRepository>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

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

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var executionStrategy = context.Database.CreateExecutionStrategy();
    await executionStrategy.ExecuteAsync(async () =>
    {
        await context.Database.MigrateAsync();
        DbInitializer.Initialize(context);
        await IdentitySeeder.InitializeAsync(scope.ServiceProvider);
    });
}

app.Run();
