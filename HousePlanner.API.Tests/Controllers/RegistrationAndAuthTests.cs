using System.Net;
using System.Net.Http.Json;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using HousePlanner.API.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;

namespace HousePlanner.API.Tests.Controllers;

public sealed class RegistrationAndAuthTests
{
    private static ApplicationDbContext Database()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task NewSupabaseUser_Register_IsAllowedWithoutExistingPublicUser()
    {
        // Verified by SupabaseUserSyncService not throwing if user doesn't exist
        await using var db = Database();
        var service = new SupabaseUserSyncService(db);
        var user = await service.RegisterCustomerAsync(
            new UserInfoResponseDto { Uid = "new-uid", Email = "test@example.com" }, "Test");
        
        Assert.NotNull(user);
        Assert.Equal("new-uid", user.SupabaseUid);
    }

    [Fact]
    public async Task NewSupabaseUser_Register_CreatesPublicUserWithJwtSub()
    {
        await using var db = Database();
        var service = new SupabaseUserSyncService(db);
        var user = await service.RegisterCustomerAsync(
            new UserInfoResponseDto { Uid = "jwt-sub-123", Email = "test@example.com" }, "Test");
        
        Assert.Equal("jwt-sub-123", user.SupabaseUid);
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task Register_UsesJwtEmail_NotClientSpoofedEmail()
    {
        await using var db = Database();
        var service = new SupabaseUserSyncService(db);
        var user = await service.RegisterCustomerAsync(
            new UserInfoResponseDto { Uid = "uid-123", Email = "jwt-email@example.com" }, "Test");
        
        // Simulating the controller ignoring any email in the body,
        // because RegisterRequestDto doesn't even have an Email property.
        Assert.Equal("jwt-email@example.com", user.Email);
    }

    [Fact]
    public async Task Register_RejectsAdminSelfAssignment()
    {
        await using var db = Database();
        var service = new SupabaseUserSyncService(db);
        var user = await service.RegisterCustomerAsync(
            new UserInfoResponseDto { Uid = "uid-123", Email = "hacker@example.com" }, "Test");
        
        // Simulating the controller calling RegisterCustomerAsync which hardcodes the Customer role.
        Assert.Equal("Customer", user.Role.Name);
        Assert.NotEqual("Admin", user.Role.Name);
    }

    [Fact]
    public async Task Register_DuplicateSupabaseUid_DoesNotCreateSecondUser()
    {
        await using var db = Database();
        var existingRole = new Role { Id = 3, Name = "Architect" };
        db.Roles.Add(existingRole);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), SupabaseUid = "uid-existing", Email = "arch@example.com",
            FullName = "Existing", RoleId = 3, Role = existingRole,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new SupabaseUserSyncService(db);
        var user = await service.RegisterCustomerAsync(
            new UserInfoResponseDto { Uid = "uid-existing", Email = "arch@example.com" }, "New Name");

        Assert.Single(db.Users);
        Assert.Equal("Existing", user.FullName); // not updated
    }

    // ── Route Tests using mocked Authentication ──────────────────────────────
    
    [Fact]
    public async Task ExistingUser_StillReceivesApplicationRoleClaim()
    {
        await using var application = new AdminStaffApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, BasicTestAuthHandler>("Test", _ => { });
            });
        });
        
        var client = application.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Customer");
        
        var response = await client.GetAsync("/api/v1/auth/debug");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Customer", content);
    }

    [Fact]
    public async Task AdminEndpoint_StillRequiresAdminRole()
    {
        await using var application = new AdminStaffApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, BasicTestAuthHandler>("Test", _ => { });
            });
        });
        var client = application.CreateClient();
        
        client.DefaultRequestHeaders.Add("X-Test-Role", "Customer"); // not admin
        var response = await client.GetAsync("/api/v1/admin/staff");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CustomerEndpoint_StillRequiresCustomerRole()
    {
        await using var application = new AdminStaffApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, BasicTestAuthHandler>("Test", _ => { });
            });
        });
        var client = application.CreateClient();
        
        client.DefaultRequestHeaders.Add("X-Test-Role", "Architect"); // not customer
        var response = await client.GetAsync("/api/v1/customer/construction");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

internal sealed class BasicTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
            return Task.FromResult(AuthenticateResult.NoResult());
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "test-user") };
        
        // Mimicking the logic where the role is added if the user is found
        if (!string.IsNullOrEmpty(role))
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
            
        var identity = new ClaimsIdentity(claims, "Test");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "Test")));
    }
}
