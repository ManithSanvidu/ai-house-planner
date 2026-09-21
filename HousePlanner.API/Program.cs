using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using HousePlanner.API.Middleware;
using HousePlanner.API.Services;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using HousePlanner.API.Data;
using HousePlanner.API.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var isTesting = builder.Environment.IsEnvironment("Testing");
if (isTesting)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
}

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnection))
{
    if (!isTesting)
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection must be configured through user secrets or environment variables.");
    defaultConnection = "Host=localhost;Database=houseplanner_tests;Username=test;Password=test";
}

var internalApiKey = builder.Configuration["AgenticService:InternalApiKey"];
if (string.IsNullOrWhiteSpace(internalApiKey))
{
    if (!isTesting)
        throw new InvalidOperationException(
            "AgenticService:InternalApiKey must be configured through user secrets or environment variables.");
    internalApiKey = "integration-test-only-key";
}

// Add PostgreSQL DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(defaultConnection));

// 1. Add CORS services allowing our React frontend client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 2. Add controllers and endpoints API exploration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthentication(FirebaseAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>(
        FirebaseAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

// 3. Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HousePlanner API",
        Version = "v1",
        Description = "Core backend Web API for AI home design and cost planning."
    });
    
    // Add Bearer token authorize options to Swagger UI for verification testing
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Firebase JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 4. Register application services
builder.Services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();
builder.Services.AddScoped<IApplicationUserSyncService, ApplicationUserSyncService>();
builder.Services.AddScoped<IFirebaseStaffAccountService, FirebaseStaffAccountService>();
builder.Services.AddScoped<IStaffAccountService, StaffAccountService>();
builder.Services.AddScoped<ApplicationRoleSeeder>();
builder.Services.AddScoped<ICurrentUserContextService, CurrentUserContextService>();
builder.Services.AddScoped<IPreDesignedPlanLayoutValidator, PreDesignedPlanLayoutValidator>();
builder.Services.AddScoped<PreDesignedPlanSeeder>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IDesignOptionsService, DesignOptionsService>();
builder.Services.AddOptions<ExternalPricingOptions>()
    .Bind(builder.Configuration.GetSection(ExternalPricingOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), "ExternalPricing:Provider is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ProviderName) && options.ProviderName.Length <= 100,
        "ExternalPricing:ProviderName is required and must not exceed 100 characters.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.FilePath), "ExternalPricing:FilePath is required.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPricingNormalizationService, PricingNormalizationService>();
builder.Services.AddScoped<FilePricingProvider>();
builder.Services.AddScoped<IExternalPricingProvider>(services =>
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalPricingOptions>>().Value;
    if (!string.Equals(options.Provider, "File", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"Unsupported external pricing provider '{options.Provider}'.");
    }

    return services.GetRequiredService<FilePricingProvider>();
});
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IConstructorWorkflowService, ConstructorWorkflowService>();
builder.Services.AddHttpClient("AgenticService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AgenticService:BaseUrl"] ?? "http://localhost:8001");
    client.Timeout = TimeSpan.FromSeconds(35);
    client.DefaultRequestHeaders.Add("X-Internal-API-Key", internalApiKey);
});

// 5. Initialize Firebase Admin SDK
var serviceAccountPath = builder.Configuration["Firebase:ServiceAccountPath"];
var fullPath = Path.Combine(builder.Environment.ContentRootPath, serviceAccountPath ?? "firebase-service-account.json");

if (!string.IsNullOrEmpty(serviceAccountPath) && File.Exists(fullPath))
{
    try
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(fullPath)
        });
        Console.WriteLine($"[Firebase SDK] Successfully initialized FirebaseApp using service account credentials from: {fullPath}");
    }
    catch (Exception ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
    {
        // WebApplicationFactory can initialize multiple test hosts in the same process.
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Firebase SDK Error] Failed to initialize FirebaseApp from file: {ex.Message}");
    }
}
else
{
    var envCreds = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
    if (!string.IsNullOrEmpty(envCreds) && File.Exists(envCreds))
    {
        try
        {
            FirebaseApp.Create();
            Console.WriteLine("[Firebase SDK] Successfully initialized FirebaseApp using GOOGLE_APPLICATION_CREDENTIALS environment variable.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Firebase SDK Error] Failed to initialize FirebaseApp from env variable: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("[Firebase SDK Warning] No Firebase service account file or environment variable found.");
        Console.WriteLine("ID Token verification will fail. Place your credentials in 'firebase-service-account.json' to test real validation.");
        
        // Attempt fallback default initialization to prevent crash if running dry or mock
        try
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.GetApplicationDefault()
            });
        }
        catch
        {
            // Suppress fallback error in console as we have already warned the developer
        }
    }
}

var app = builder.Build();

// Apply CORS Policy early to ensure all responses (including errors) get the headers
app.UseCors("AllowReactApp");

// Apply checked-in migrations without deleting persisted designs. Integration tests
// exercise routing with substituted services and do not need a database connection.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    await scope.ServiceProvider.GetRequiredService<ApplicationRoleSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<PreDesignedPlanSeeder>().SeedAsync();
}

// 6. Register exception-handling middleware early in request pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Enable Swagger in Development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HousePlanner API v1");
        c.RoutePrefix = "swagger"; // Exposes Swagger at http://localhost:<port>/swagger
    });
}

// Disable default HTTPS redirect for ease of local testing in CORS environments if desired,
// but keep it active and ensure client URLs match.
if (!isTesting)
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Python callbacks are internal service-to-service requests.
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/v1/internal"), branch =>
{
    branch.Use(async (context, next) =>
    {
        if (!context.Request.Headers.TryGetValue("X-Internal-API-Key", out var actual) || actual != internalApiKey)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        await next();
    });
});

// Map controllers
app.MapControllers();

app.Run();

public partial class Program;
