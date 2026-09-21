using HousePlanner.API.Data;
using HousePlanner.API.Models;
using HousePlanner.API.Options;
using HousePlanner.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HousePlanner.API.Tests.Services;

public class PricingSyncTests
{
    [Fact]
    public async Task Sync_PersistsNormalizedPriceAndFullProvenance()
    {
        await using var context = CreateContext();
        var observedAt = new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero);
        var provider = new StubProvider([CreateRecord(2500m, observedAt)]);
        var service = CreateService(context, provider);

        var result = await service.SyncExternalPricingAsync();

        Assert.Equal(1, result.ImportedCount);
        var item = await context.PricingItems.SingleAsync();
        Assert.Equal("material", item.Category);
        Assert.Equal("per_sqft", item.Unit);
        Assert.Equal(200m, item.UnitCostLkr);
        Assert.Equal("TestFeed", item.Provider);
        Assert.Equal("cement-001", item.ExternalItemId);
        Assert.Equal("50 kg cement bag", item.ExternalItemName);
        Assert.Equal("bag", item.OriginalUnit);
        Assert.Equal(2500m, item.OriginalPrice);
        Assert.Equal("Western", item.Region);
        Assert.Equal(observedAt, item.ObservedAt);
        Assert.Equal("https://prices.example/items/1", item.SourceUrl);
        Assert.NotNull(item.ImportedAt);
        var dto = await service.GetPricingByIdAsync(item.Id);
        Assert.Equal(item.UpdatedAt, dto!.UpdatedAt);
        Assert.Equal("succeeded", (await context.PricingImportAudits.SingleAsync()).Status);
    }

    [Fact]
    public async Task Sync_UpsertsExistingProviderItem()
    {
        await using var context = CreateContext();
        var provider = new StubProvider([CreateRecord(2500m)]);
        var service = CreateService(context, provider);
        await service.SyncExternalPricingAsync();

        provider.Records = [CreateRecord(3000m)];
        var result = await service.SyncExternalPricingAsync();

        Assert.Equal(1, result.ImportedCount);
        var item = await context.PricingItems.SingleAsync();
        Assert.Equal(3000m, item.OriginalPrice);
        Assert.Equal(240m, item.UnitCostLkr);
        Assert.Equal(2, await context.PricingImportAudits.CountAsync());
    }

    [Fact]
    public async Task Sync_EmptyProviderResponseCreatesSuccessfulAudit()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new StubProvider([]));

        var result = await service.SyncExternalPricingAsync();

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(context.PricingItems);
        Assert.Equal("succeeded", (await context.PricingImportAudits.SingleAsync()).Status);
    }

    [Fact]
    public async Task Sync_ProviderFailurePersistsFailedAuditAndReportsFailure()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new StubProvider(new ExternalPricingProviderException("Feed unavailable")));

        var exception = await Assert.ThrowsAsync<PricingSyncException>(() => service.SyncExternalPricingAsync());

        Assert.Equal(1, exception.Result.FailedCount);
        var audit = await context.PricingImportAudits.SingleAsync();
        Assert.Equal("failed", audit.Status);
        Assert.Equal("Feed unavailable", audit.FailureMessage);
    }

    [Fact]
    public async Task Sync_SkipsRecordWithoutConfiguredUnitRule()
    {
        await using var context = CreateContext();
        var provider = new StubProvider([new ExternalPriceRecord
        {
            ExternalItemId = "steel-001",
            Name = "Steel cube",
            Unit = "cube",
            Price = 1000m
        }]);
        var service = CreateService(context, provider);

        var result = await service.SyncExternalPricingAsync();

        Assert.Equal(1, result.SkippedCount);
        Assert.Empty(context.PricingItems);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static PricingService CreateService(ApplicationDbContext context, IExternalPricingProvider provider)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalPricingOptions
        {
            ConversionRules =
            [
                new PricingConversionRule
                {
                    ExternalItemId = "cement-001",
                    OriginalUnit = "bag",
                    Category = "material",
                    TargetUnit = "per_sqft",
                    PriceMultiplier = 0.08m,
                    DisplayGroup = "Concrete"
                }
            ]
        });
        return new PricingService(context, provider, new PricingNormalizationService(options), TimeProvider.System);
    }

    private static ExternalPriceRecord CreateRecord(decimal price, DateTimeOffset? observedAt = null) => new()
    {
        ExternalItemId = "cement-001",
        Name = "50 kg cement bag",
        Unit = "bag",
        Price = price,
        Currency = "LKR",
        Region = "Western",
        ObservedAt = observedAt,
        EffectiveAt = observedAt,
        SourceUrl = "https://prices.example/items/1",
        SourceReference = "approved-feed-1"
    };

    private sealed class StubProvider : IExternalPricingProvider
    {
        private readonly Exception? _exception;

        public StubProvider(IReadOnlyList<ExternalPriceRecord> records) => Records = records;
        public StubProvider(Exception exception) => _exception = exception;
        public string Name => "TestFeed";
        public IReadOnlyList<ExternalPriceRecord> Records { get; set; } = [];

        public Task<IReadOnlyList<ExternalPriceRecord>> GetPricesAsync(CancellationToken cancellationToken = default) =>
            _exception is null
                ? Task.FromResult(Records)
                : Task.FromException<IReadOnlyList<ExternalPriceRecord>>(_exception);
    }
}
