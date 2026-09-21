using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HousePlanner.API.Controllers;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HousePlanner.API.Tests.Controllers
{
    public class InternalPricingControllerTests
    {
        private readonly Mock<IPricingService> _pricingServiceMock;
        private readonly InternalPricingController _controller;

        public InternalPricingControllerTests()
        {
            _pricingServiceMock = new Mock<IPricingService>();
            _controller = new InternalPricingController(_pricingServiceMock.Object);
        }

        [Fact]
        public async Task GetPricing_ReturnsOkResult_WithPricingCollection()
        {
            // Arrange
            var mockPricing = new List<PricingDto>
            {
                new()
                {
                    Id = 1,
                    ItemName = "Concrete Foundation",
                    Category = "material",
                    UnitCostLkr = 5000.00m,
                    Unit = "per_sqft",
                    TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.25m, Coastal = 1.15m }
                },
                new()
                {
                    Id = 2,
                    ItemName = "Brick Wall",
                    Category = "material",
                    UnitCostLkr = 3500.00m,
                    Unit = "per_sqft",
                    TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.1m, Coastal = 1.2m }
                }
            };

            _pricingServiceMock.Setup(s => s.GetAllPricingAsync())
                .ReturnsAsync(mockPricing);

            // Act
            var actionResult = await _controller.GetPricing();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedItems = Assert.IsAssignableFrom<IEnumerable<PricingDto>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count());
        }

        [Fact]
        public async Task GetPricing_ReturnsOkResult_WhenCollectionIsEmpty()
        {
            // Arrange
            _pricingServiceMock.Setup(s => s.GetAllPricingAsync())
                .ReturnsAsync(new List<PricingDto>());

            // Act
            var actionResult = await _controller.GetPricing();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedItems = Assert.IsAssignableFrom<IEnumerable<PricingDto>>(okResult.Value);
            Assert.Empty(returnedItems);
        }
    }
}
