using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace HousePlanner.API.Tests.Controllers;

public sealed class PricingRouteTests : IClassFixture<PricingApiFactory>
{
    private readonly HttpClient _client;

    public PricingRouteTests(PricingApiFactory factory) => _client = factory.CreateClient();

    // ──────────────────────────────────────────────
    // GET /api/v1/pricing  — open (no auth required)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllPricing_IsPublic_ReturnsOk()
    {
        // No auth header — should still succeed
        var response = await _client.GetAsync("/api/v1/pricing");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    // POST /api/v1/pricing  — Constructor only
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreatePricing_Unauthenticated_Returns401()
    {
        // No X-Test-Role header → no identity → 401
        var response = await _client.PostAsJsonAsync("/api/v1/pricing", ValidCreateDto());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Architect")]
    public async Task CreatePricing_NonConstructorRole_Returns403(string role)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pricing");
        request.Headers.Add("X-Test-Role", role);
        request.Content = JsonContent.Create(ValidCreateDto());

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatePricing_ConstructorRole_Returns201()
    {
        using var request = ConstructorRequest(HttpMethod.Post, "/api/v1/pricing");
        request.Content = JsonContent.Create(ValidCreateDto());

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    // PUT /api/v1/pricing/{id}  — Constructor only
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdatePricing_Unauthenticated_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/api/v1/pricing/1", ValidUpdateDto());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Architect")]
    public async Task UpdatePricing_NonConstructorRole_Returns403(string role)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/pricing/1");
        request.Headers.Add("X-Test-Role", role);
        request.Content = JsonContent.Create(ValidUpdateDto());

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static HttpRequestMessage ConstructorRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Test-Role", "Constructor");
        return request;
    }

    private static CreatePricingDto ValidCreateDto() => new()
    {
        ItemName = "Route Test Material",
        Category = "material",
        UnitCostLkr = 1000m,
        TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.2m, Coastal = 1.3m }
    };

    private static UpdatePricingDto ValidUpdateDto() => new()
    {
        UnitCostLkr = 2000m,
        TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.2m, Coastal = 1.3m }
    };
}

public sealed class PricingApiFactory : WebApplicationFactory<Program>
{
    public PricingApiFactory()
    {
        Environment.SetEnvironmentVariable("DATABASE_CONNECTION_STRING", "Host=localhost;Database=test;Username=postgres;Password=postgres");
        Environment.SetEnvironmentVariable("SUPABASE_JWT_SECRET", "super-secret-jwt-key-that-is-at-least-32-bytes-long-for-testing");
        Environment.SetEnvironmentVariable("SUPABASE_URL", "http://localhost:8000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            // Replace auth with the lightweight test scheme
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = PricingTestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = PricingTestAuthHandler.AuthenticationScheme;
                    options.DefaultForbidScheme = PricingTestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, PricingTestAuthHandler>(PricingTestAuthHandler.AuthenticationScheme, _ => { });

            services.AddSingleton<ICurrentUserContextService, PricingTestCurrentUserContext>();

            // Stub the pricing service so the route layer is tested without DB
            var pricingServiceMock = new Mock<IPricingService>();
            pricingServiceMock
                .Setup(s => s.GetAllPricingAsync())
                .ReturnsAsync(Array.Empty<PricingDto>());
            pricingServiceMock
                .Setup(s => s.CreatePricingAsync(It.IsAny<CreatePricingDto>()))
                .ReturnsAsync(new PricingDto
                {
                    Id = 1,
                    ItemName = "Route Test Material",
                    Category = "material",
                    Unit = "per_sqft",
                    UnitCostLkr = 1000m,
                    Provider = "Manual",
                    TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.2m, Coastal = 1.3m }
                });
            services.AddSingleton(pricingServiceMock.Object);
        });
    }
}

internal sealed class PricingTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "PricingRouteTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "pricing-test-user"), new Claim(ClaimTypes.Role, role.ToString())],
            AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationScheme)));
    }
}

internal sealed class PricingTestCurrentUserContext : ICurrentUserContextService
{
    public Task<CurrentUserContext?> GetAsync(HttpContext context)
    {
        var role = context.User.FindFirstValue(ClaimTypes.Role);
        return Task.FromResult<CurrentUserContext?>(role is null
            ? null
            : new CurrentUserContext(Guid.Parse("33333333-3333-3333-3333-333333333333"), "pricing@example.com", role));
    }
}
