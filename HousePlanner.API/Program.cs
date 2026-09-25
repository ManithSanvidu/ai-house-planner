using DotNetEnv;
using HousePlanner.API.Middleware;
using HousePlanner.API.Services;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using HousePlanner.API.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var isTesting = builder.Environment.IsEnvironment("Testing");
if (isTesting)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
}

// Load .env variables (Important to do this early)
if (!isTesting)
    Env.Load();

// Safely retrieve the connection string (Prioritize .env over appsettings.json)
var defaultConnection = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(defaultConnection))
{
    if (!isTesting)
        throw new InvalidOperationException(
            "DATABASE_CONNECTION_STRING environment variable or ConnectionStrings:DefaultConnection must be configured.");
    defaultConnection = "Host=localhost;Database=houseplanner_tests;Username=test;Password=test";
}

if (!isTesting)
    Console.WriteLine("[Database Configuration] Connection string loaded successfully.");

var internalApiKey = builder.Configuration["AgenticService:InternalApiKey"]
    ?? Environment.GetEnvironmentVariable("AGENTIC_INTERNAL_API_KEY");
if (string.IsNullOrWhiteSpace(internalApiKey))
{
    if (!isTesting)
        throw new InvalidOperationException(
            "AgenticService:InternalApiKey must be configured through user secrets or environment variables.");
    internalApiKey = "integration-test-only-key";
}

// Add PostgreSQL DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        defaultConnection,
        npgsql =>
        {
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);

            npgsql.CommandTimeout(60);
        }));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Controllers and API exploration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddAuthentication(
        Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var supabaseUrl =
            Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? "https://cqfelbazvvbeiwwydwmn.supabase.co";

        var supabaseJwtSecret =
            Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET");

        options.MapInboundClaims = false;

        options.Authority = $"{supabaseUrl}/auth/v1";

        options.TokenValidationParameters =
            new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"{supabaseUrl}/auth/v1",

                ValidateAudience = true,
                ValidAudience = "authenticated",

                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(
                            supabaseJwtSecret ?? string.Empty)),

                RoleClaimType = ClaimTypes.Role,
                NameClaimType = "sub"
            };

        options.Events =
            new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    Console.WriteLine("========== TOKEN VALIDATED ==========");

                    var uid = context.Principal?
                        .FindFirst("sub")?.Value;

                    if (string.IsNullOrEmpty(uid))
                    {
                        Console.WriteLine(
                            "[Auth] Missing Supabase sub claim — rejecting.");

                        context.Fail("Missing Supabase sub claim.");
                        return;
                    }

                    Console.WriteLine($"[Auth] Supabase UID: {uid}");

                    var db = context.HttpContext.RequestServices
                        .GetRequiredService<ApplicationDbContext>();

                    var user = await db.Users
                        .Include(u => u.Role)
                        .SingleOrDefaultAsync(u => u.SupabaseUid == uid);

                    if (user?.Role != null)
                    {
                        Console.WriteLine(
                            $"[Auth] Application role: {user.Role.Name}");

                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            identity.AddClaim(
                                new Claim(
                                    ClaimTypes.Role,
                                    user.Role.Name));
                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            $"[Auth] Application user not found for UID: {uid}");
                    }
                }
            };
    });

builder.Services.AddAuthorization();

// Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HousePlanner API",
        Version = "v1",
        Description =
            "Core backend Web API for AI home design and cost planning."
    });

    // Add Bearer token authorize options to Swagger UI for verification testing
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "Supabase JWT Authorization header using the Bearer scheme. " +
            "Example: \"Authorization: Bearer {token}\"",

        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
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

// Application services
builder.Services.AddScoped<ISupabaseUserSyncService, SupabaseUserSyncService>();
builder.Services.AddScoped<ISupabaseStaffAccountService, SupabaseStaffAccountService>();
builder.Services.AddScoped<IStaffAccountService, StaffAccountService>();
builder.Services.AddScoped<ApplicationRoleSeeder>();
builder.Services.AddScoped<ICurrentUserContextService, CurrentUserContextService>();
builder.Services.AddScoped<IPreDesignedPlanLayoutValidator, PreDesignedPlanLayoutValidator>();
builder.Services.AddScoped<PreDesignedPlanSeeder>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IDesignOptionsService, DesignOptionsService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PricingDataSeeder>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IConstructorWorkflowService, ConstructorWorkflowService>();
builder.Services.AddScoped<IDailyConstructionLogService, DailyConstructionLogService>();

// AgenticService HTTP client — internalApiKey validated and injected at startup (never falls back to a plain default)
builder.Services.AddHttpClient("AgenticService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AgenticService:BaseUrl"]
        ?? "http://localhost:8001");

    client.Timeout = TimeSpan.FromSeconds(35);
    client.DefaultRequestHeaders.Add("X-Internal-API-Key", internalApiKey);
});

// Supabase Admin API
var supabaseAdminUrl =
    Environment.GetEnvironmentVariable("SUPABASE_URL")
    ?? "https://cqfelbazvvbeiwwydwmn.supabase.co";

var serviceRoleKey =
    Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
    ?? string.Empty;

if (string.IsNullOrWhiteSpace(serviceRoleKey))
{
    Console.WriteLine(
        "[Config WARNING] SUPABASE_SERVICE_ROLE_KEY is not set. " +
        "Admin staff creation will fail.");
}

builder.Services.AddHttpClient("SupabaseAdmin", client =>
{
    client.BaseAddress =
        new Uri($"{supabaseAdminUrl}/auth/v1/");

    client.Timeout = TimeSpan.FromSeconds(30);

    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            serviceRoleKey);

    client.DefaultRequestHeaders.Add(
        "apikey",
        serviceRoleKey);
});

var app = builder.Build();

// CORS
app.UseCors("AllowReactApp");

// Apply checked-in migrations without deleting persisted designs. Integration tests
// exercise routing with substituted services and do not need a database connection.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();

    var context =
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var maxRetries = 3;

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            context.Database.Migrate();
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Startup Migration] Attempt {attempt} failed: {ex.Message}");

            if (attempt == maxRetries)
                throw;

            Thread.Sleep(3000);
        }
    }
    await scope.ServiceProvider.GetRequiredService<ApplicationRoleSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<PreDesignedPlanSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<PricingDataSeeder>().SeedAsync();
}

// Exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Request logging
app.Use(async (context, next) =>
{
    Console.WriteLine($"Path: {context.Request.Path}");

    Console.WriteLine(
        $"Authorization header present: " +
        $"{context.Request.Headers.ContainsKey("Authorization")}");

    await next();
});

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "HousePlanner API v1");

        c.RoutePrefix = "swagger";
    });
}

// Authentication and authorization
// HTTPS redirection is intentionally disabled because
// Render terminates HTTPS at its proxy.
app.UseAuthentication();
app.UseAuthorization();

// Internal Agentic Service callbacks
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/v1/internal"),
    branch =>
    {
        branch.Use(async (context, next) =>
        {
            if (!context.Request.Headers.TryGetValue(
                    "X-Internal-API-Key",
                    out var actual)
                || actual != internalApiKey)
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                return;
            }

            await next();
        });
    });

// API Controllers
app.MapControllers();

// Root endpoint
app.MapGet("/", () =>
    Results.Ok(new
    {
        message = "AI House Planner API is running",
        status = "healthy"
    }));

// Health endpoint
app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "healthy"
    }));

app.Run();

public partial class Program;
