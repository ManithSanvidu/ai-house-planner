using DotNetEnv;
using HousePlanner.API.Middleware;
using HousePlanner.API.Services;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using HousePlanner.API.Data;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// 1. Load .env variables (Important to do this early)
Env.Load();

// 2. Safely retrieve the connection string (Prioritize .env over appsettings.json)
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("DATABASE_CONNECTION_STRING environment variable is missing or empty.");
}

Console.WriteLine("[Database Configuration] Connection string loaded successfully.");

// 3. Add PostgreSQL DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsql =>
        {
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);

            npgsql.CommandTimeout(60);
        }));

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
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "https://cqfelbazvvbeiwwydwmn.supabase.co";
        var supabaseJwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET");

        // CRITICAL: Disable claim remapping so "sub" stays as "sub" and is not
        // renamed to ClaimTypes.NameIdentifier by the JWT middleware.
        options.MapInboundClaims = false;

        options.Authority = $"{supabaseUrl}/auth/v1";

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(supabaseJwtSecret ?? string.Empty)),
            // Tell ASP.NET which claim carries the application role.
            RoleClaimType = ClaimTypes.Role,
            // Prevent "sub" from being remapped at the token validation level.
            NameClaimType = "sub"
        };

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                Console.WriteLine("========== TOKEN VALIDATED ==========");

                // With MapInboundClaims = false, "sub" is preserved as-is.
                var uid = context.Principal?.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(uid))
                {
                    Console.WriteLine("[Auth] Missing Supabase sub claim — rejecting.");
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
                    Console.WriteLine($"[Auth] Application role: {user.Role.Name}");
                    if (context.Principal?.Identity is ClaimsIdentity identity)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.Name));
                    }
                }
                else
                {
                    Console.WriteLine($"[Auth] Application user not found for UID: {uid}");
                }
            }
        };
    });
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
        Description = "Supabase JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
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
builder.Services.AddScoped<ISupabaseUserSyncService, SupabaseUserSyncService>();
builder.Services.AddScoped<ISupabaseStaffAccountService, SupabaseStaffAccountService>();
builder.Services.AddScoped<IStaffAccountService, StaffAccountService>();
builder.Services.AddScoped<ApplicationRoleSeeder>();
builder.Services.AddScoped<ICurrentUserContextService, CurrentUserContextService>();
builder.Services.AddScoped<IPreDesignedPlanLayoutValidator, PreDesignedPlanLayoutValidator>();
builder.Services.AddScoped<PreDesignedPlanSeeder>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IDesignOptionsService, DesignOptionsService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IConstructorWorkflowService, ConstructorWorkflowService>();
builder.Services.AddScoped<IDailyConstructionLogService, DailyConstructionLogService>();
builder.Services.AddHttpClient("AgenticService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AgenticService:BaseUrl"] ?? "http://localhost:8001");
    client.Timeout = TimeSpan.FromSeconds(35);
    client.DefaultRequestHeaders.Add("X-Internal-API-Key",
        builder.Configuration["AgenticService:InternalApiKey"] ?? "shared-internal-secret");
});

// Supabase Admin API HTTP client - uses service role key (server-only, never exposed to browser)
var supabaseAdminUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "https://cqfelbazvvbeiwwydwmn.supabase.co";
var serviceRoleKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY") ?? string.Empty;
if (string.IsNullOrWhiteSpace(serviceRoleKey))
    Console.WriteLine("[Config WARNING] SUPABASE_SERVICE_ROLE_KEY is not set. Admin staff creation will fail.");
builder.Services.AddHttpClient("SupabaseAdmin", client =>
{
    client.BaseAddress = new Uri($"{supabaseAdminUrl}/auth/v1/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceRoleKey);
    client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
});

var app = builder.Build();

// Apply CORS Policy early to ensure all responses (including errors) get the headers
app.UseCors("AllowReactApp");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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
            Console.WriteLine($"[Startup Migration] Attempt {attempt} failed: {ex.Message}");
            if (attempt == maxRetries) throw;
            Thread.Sleep(3000);
        }
    }
    await scope.ServiceProvider.GetRequiredService<ApplicationRoleSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<PreDesignedPlanSeeder>().SeedAsync();
}

// 6. Register exception-handling middleware early in request pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Use(async (context, next) =>
{
    Console.WriteLine($"Path: {context.Request.Path}");
    Console.WriteLine($"Authorization header present: {context.Request.Headers.ContainsKey("Authorization")}");
    await next();
});

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
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Python callbacks are internal service-to-service requests.
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/v1/internal"), branch =>
{
    branch.Use(async (context, next) =>
    {
        var expected = builder.Configuration["AgenticService:InternalApiKey"] ?? "shared-internal-secret";
        if (!context.Request.Headers.TryGetValue("X-Internal-API-Key", out var actual) || actual != expected)
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
