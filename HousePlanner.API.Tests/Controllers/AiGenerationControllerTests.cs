using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using HousePlanner.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace HousePlanner.API.Tests.Controllers
{
    public class AiGenerationControllerTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly AiGenerationController _controller;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<IDesignOptionsService> _mockDesignOptionsService;

        public AiGenerationControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _dbContext = new ApplicationDbContext(options);

            // Add a mock client to avoid foreign key/user null checks
            _dbContext.Users.Add(new User { Id = Guid.NewGuid(), Email = "test@example.com", FullName = "Test User", PasswordHash = "dummy" });
            _dbContext.SaveChanges();

            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://localhost:8001")
            };
            
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient("AgenticService")).Returns(httpClient);

            _mockDesignOptionsService = new Mock<IDesignOptionsService>();
            
            _controller = new AiGenerationController(_dbContext, httpClientFactory.Object, _mockDesignOptionsService.Object);
        }

        [Fact]
        public async Task Generate_HappyPath_CreatesWorkflowAndReturns200()
        {
            // Arrange
            var req = new AiGenerationRequest
            {
                LandSizePerches = 15,
                Preferences = new PreferencesDto { Bedrooms = 3, Bathrooms = 2, ArchitecturalStyle = "Modern Minimalist" }
            };

            _mockDesignOptionsService.Setup(s => s.ValidateFinalSelectionAsync(req, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DesignOptionsValidationResult { IsValid = true });

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

            // Act
            var result = await _controller.Generate(req, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(okResult.Value));
            Assert.True(response.TryGetProperty("WorkflowId", out var workflowIdProp));
            
            var workflowId = workflowIdProp.GetGuid();
            var workflow = await _dbContext.WorkflowStates.FindAsync(workflowId);
            Assert.NotNull(workflow);
            Assert.Equal("running", workflow.Status);
        }

        [Fact]
        public async Task Generate_UnsupportedConfiguration_Returns400WithSuggestions()
        {
            // Arrange
            var req = new AiGenerationRequest
            {
                LandSizePerches = 15,
                Preferences = new PreferencesDto { Bedrooms = 3, ParkingRequired = true }
            };

            var validationResult = new DesignOptionsValidationResult
            {
                IsValid = false,
                ErrorCode = "UNSUPPORTED_DESIGN_CONFIGURATION",
                Conflicts = new List<string> { "parking" },
                Suggestions = new List<SuggestionDto> {
                    new SuggestionDto("parkingRequired", false, "Remove parking")
                }
            };

            _mockDesignOptionsService.Setup(s => s.ValidateFinalSelectionAsync(req, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _controller.Generate(req, CancellationToken.None);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(badRequest.Value));
            
            Assert.Equal("UNSUPPORTED_DESIGN_CONFIGURATION", response.GetProperty("code").GetString());
            var conflicts = response.GetProperty("conflicts").EnumerateArray().Select(e => e.GetString()).ToList();
            Assert.Contains("parking", conflicts);
            
            var suggestions = response.GetProperty("suggestions").EnumerateArray();
            Assert.Equal("parkingRequired", suggestions.First().GetProperty("field").GetString());

            // Ensure no workflow was created in DB
            Assert.Empty(_dbContext.WorkflowStates);
        }
    }
}
