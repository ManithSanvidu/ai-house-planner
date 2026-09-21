using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace HousePlanner.API.Tests.Controllers;

public class CustomerConstructionRouteIntegrationTests : IClassFixture<CustomerConstructionApiFactory>
{
    private readonly CustomerConstructionApiFactory _factory;

    public CustomerConstructionRouteIntegrationTests(CustomerConstructionApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApprovedDesignsRoute_IsConfiguredCorrectly_AndNot404()
    {
        var client = _factory.CreateClient();
        
        // The endpoint we are testing
        var response = await client.GetAsync("/api/v1/customer/construction/approved-designs");
        
        // We expect either 200 OK (if auth was bypassed/mocked) or 401 Unauthorized, but DEFINITELY NOT 404 Not Found.
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task ConstructorsRoute_IsConfiguredCorrectly_AndNot404()
    {
        var client = _factory.CreateClient();
        
        // The endpoint we are testing
        var response = await client.GetAsync("/api/v1/customer/construction/constructors");
        
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class CustomerConstructionApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment("Testing");
}
