using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using HousePlanner.API.DTOs;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HousePlanner.API.Tests.Controllers;

public sealed class AdminStaffRouteTests : IClassFixture<AdminStaffApiFactory>
{
    private readonly HttpClient _client;

    public AdminStaffRouteTests(AdminStaffApiFactory factory) => _client = factory.CreateClient();

    [Theory]
    [InlineData("/api/v1/admin/staff")]
    [InlineData("/api/v1/admin/staff?role=Architect")]
    [InlineData("/api/v1/admin/staff?role=Constructor")]
    public async Task AdminCanReachStaffListRoutes(string path)
    {
        using var request = AdminRequest(HttpMethod.Get, path);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminCanReachCreateRoute()
    {
        using var request = AdminRequest(HttpMethod.Post, "/api/v1/admin/staff");
        request.Content = JsonContent.Create(new CreateStaffRequestDto
            { FullName = "Test Architect", Email = "architect@example.com", Password = "secret1", Role = "Architect" });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AdminCanReachStatusRoute()
    {
        var id = Guid.NewGuid();
        using var request = AdminRequest(HttpMethod.Patch, $"/api/v1/admin/staff/{id}/status");
        request.Content = JsonContent.Create(new UpdateStaffStatusDto { Disabled = true });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminCanReachUpdateRoute()
    {
        var id = Guid.NewGuid();
        using var request = AdminRequest(HttpMethod.Put, $"/api/v1/admin/staff/{id}");
        request.Content = JsonContent.Create(new UpdateStaffRequestDto
            { FullName = "Updated Staff", Email = "updated@example.com", Role = "Constructor" });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Architect")]
    [InlineData("Constructor")]
    public async Task NonAdminCannotReachUpdateRoute(string role)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"/api/v1/admin/staff/{Guid.NewGuid()}");
        request.Headers.Add("X-Test-Role", role);
        request.Content = JsonContent.Create(new UpdateStaffRequestDto
            { FullName = "Updated Staff", Email = "updated@example.com", Role = "Architect" });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedCannotReachUpdateRoute()
    {
        var response = await _client.PutAsJsonAsync($"/api/v1/admin/staff/{Guid.NewGuid()}",
            new UpdateStaffRequestDto { FullName = "Updated", Email = "updated@example.com", Role = "Architect" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MissingAuthenticationReturns401InsteadOf404()
    {
        var response = await _client.GetAsync("/api/v1/admin/staff");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedNonAdminReturns403InsteadOf404()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/staff");
        request.Headers.Add("X-Test-Role", "Customer");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRoleFilterReturns400InsteadOf404()
    {
        using var request = AdminRequest(HttpMethod.Get, "/api/v1/admin/staff?role=Customer");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static HttpRequestMessage AdminRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Test-Role", "Admin");
        return request;
    }
}

public sealed class AdminStaffApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultForbidScheme = TestAuthenticationHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationScheme, _ => { });
            services.AddSingleton<ICurrentUserContextService, TestCurrentUserContext>();
            services.AddSingleton<IStaffAccountService, TestStaffAccountService>();
        });
    }
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "RouteTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
            return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "route-test-user"), new Claim(ClaimTypes.Role, role.ToString())], AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationScheme)));
    }
}

internal sealed class TestCurrentUserContext : ICurrentUserContextService
{
    public Task<CurrentUserContext?> GetAsync(HttpContext context)
    {
        var role = context.User.FindFirstValue(ClaimTypes.Role);
        return Task.FromResult<CurrentUserContext?>(role is null
            ? null
            : new CurrentUserContext(Guid.Parse("11111111-1111-1111-1111-111111111111"), "admin@example.com", role));
    }
}

internal sealed class TestStaffAccountService : IStaffAccountService
{
    private static StaffAccountDto Account(string role = "Architect") =>
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Test Staff", "staff@example.com", role, "Active");

    public Task<IReadOnlyList<StaffAccountDto>> ListAsync(string? role, CancellationToken cancellationToken = default) =>
        role is null || role is "Architect" or "Constructor"
            ? Task.FromResult<IReadOnlyList<StaffAccountDto>>([Account(role ?? "Architect")])
            : Task.FromException<IReadOnlyList<StaffAccountDto>>(
                new StaffAccountException("invalid_request", "Role must be Architect or Constructor."));

    public Task<StaffAccountDto> CreateAsync(CreateStaffRequestDto request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Account(request.Role));

    public Task<StaffAccountDto> UpdateAsync(Guid id, UpdateStaffRequestDto request,
        CancellationToken cancellationToken = default) => Task.FromResult(Account(request.Role));

    public Task<StaffAccountDto> SetDisabledAsync(Guid id, bool disabled, CancellationToken cancellationToken = default) =>
        Task.FromResult(Account());
}
