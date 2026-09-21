using HousePlanner.API.Options;
using HousePlanner.API.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace HousePlanner.API.Tests.Services;

public class FilePricingProviderTests
{
    [Fact]
    public async Task GetPricesAsync_LoadsApprovedJsonFeed()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, """
                {
                  "records": [{
                    "externalItemId": "cement-001",
                    "name": "Cement",
                    "unit": "bag",
                    "price": 2450.50,
                    "currency": "LKR",
                    "region": "Western",
                    "sourceReference": "approved-feed-1"
                  }]
                }
                """);
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(value => value.ContentRootPath).Returns(Path.GetDirectoryName(path)!);
            environment.SetupGet(value => value.ContentRootFileProvider).Returns(Mock.Of<IFileProvider>());
            var provider = new FilePricingProvider(Microsoft.Extensions.Options.Options.Create(new ExternalPricingOptions
            {
                ProviderName = "TestFeed",
                FilePath = path
            }), environment.Object);

            var records = await provider.GetPricesAsync();

            var record = Assert.Single(records);
            Assert.Equal("cement-001", record.ExternalItemId);
            Assert.Equal("bag", record.Unit);
            Assert.Equal(2450.50m, record.Price);
            Assert.Equal("Western", record.Region);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
