using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using HousePlanner.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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
        private readonly Guid _clientId = Guid.NewGuid();

        public AiGenerationControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _dbContext = new ApplicationDbContext(options);

            // Add a mock client to avoid foreign key/user null checks
            _dbContext.Users.Add(new User { Id = _clientId, Email = "test@example.com", FullName = "Test User", PasswordHash = "dummy" });
            _dbContext.SaveChanges();

            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://localhost:8001")
            };
            
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient("AgenticService")).Returns(httpClient);

            _mockDesignOptionsService = new Mock<IDesignOptionsService>();
            
            var currentUser = new Mock<ICurrentUserContextService>();
            currentUser.Setup(x => x.GetAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(new CurrentUserContext(_clientId, "test@example.com", "Customer"));
            _controller = new AiGenerationController(_dbContext, httpClientFactory.Object,
                _mockDesignOptionsService.Object, currentUser.Object);
        }

        [Fact]
        public async Task Generate_IgnoresSpoofedClientIdAndUsesAuthenticatedCustomer()
        {
            var otherCustomer = new User { Id = Guid.NewGuid(), Email = "other@example.com", FullName = "Other" };
            _dbContext.Users.Add(otherCustomer);
            await _dbContext.SaveChangesAsync();
            var request = new AiGenerationRequest
            {
                ClientId = otherCustomer.Id, LandSizePerches = 15,
                Preferences = new PreferencesDto { Bedrooms = 3, Bathrooms = 2, Floors = 1 }
            };
            _mockDesignOptionsService.Setup(s => s.ValidateFinalSelectionAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DesignOptionsValidationResult { IsValid = true });
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            Assert.IsType<OkObjectResult>(await _controller.Generate(request, CancellationToken.None));
            Assert.Equal(_clientId, (await _dbContext.LandSubmissions.SingleAsync()).ClientId);
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

        [Fact]
        public async Task Generate_SelectedPlanIsRevalidatedBeforeWorkflowCreation()
        {
            var plan = new PreDesignedHousePlan { Name="Selected",Slug="selected",DesignCode="SELECTED",
                Style="Modern",Bedrooms=2,Bathrooms=1,FloorCount=1,MinimumLandSizePerches=8,
                SuitableTerrain="flat",TagsJson="[]",LayoutJson="{}",IsActive=true };
            _dbContext.PreDesignedHousePlans.Add(plan); await _dbContext.SaveChangesAsync();
            var request = new AiGenerationRequest { BasePreDesignedPlanId=plan.Id,PlanSelectionMode="use",
                LandSizePerches=10,ManualTerrainType="flat",Preferences=new PreferencesDto{Bedrooms=3,Bathrooms=2,Floors=1} };
            _mockDesignOptionsService.Setup(x=>x.ValidateFinalSelectionAsync(request,It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DesignOptionsValidationResult{IsValid=true});
            _mockDesignOptionsService.Setup(x=>x.ValidateSpecificPlanAsync(plan,request,It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DesignOptionsValidationResult{IsValid=false,ErrorCode="SELECTED_PLAN_INCOMPATIBLE",Message="Incompatible",Conflicts=["bedrooms"]});
            var result=Assert.IsType<BadRequestObjectResult>(await _controller.Generate(request,CancellationToken.None));
            Assert.Empty(_dbContext.WorkflowStates);
        }

        [Fact]
        public async Task Generate_CompatibleSelectedPlanReachesAiAsServerResolvedPlanCode()
        {
            var plan = new PreDesignedHousePlan { Name="Selected",Slug="selected-ai",DesignCode="SELECTED-AI",
                Style="Modern",Bedrooms=2,Bathrooms=1,FloorCount=1,MinimumLandSizePerches=8,
                SuitableTerrain="flat",TagsJson="[]",LayoutJson="{}",IsActive=true };
            _dbContext.PreDesignedHousePlans.Add(plan); await _dbContext.SaveChangesAsync();
            var request = new AiGenerationRequest { BasePreDesignedPlanId=plan.Id,PlanSelectionMode="use",
                LandSizePerches=10,ManualTerrainType="flat",Preferences=new PreferencesDto{Bedrooms=2,Bathrooms=1,Floors=1,ArchitecturalStyle="Modern"} };
            _mockDesignOptionsService.Setup(x=>x.ValidateFinalSelectionAsync(request,It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DesignOptionsValidationResult{IsValid=true});
            _mockDesignOptionsService.Setup(x=>x.ValidateSpecificPlanAsync(plan,request,It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DesignOptionsValidationResult{IsValid=true});
            string? body=null;
            _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",ItExpr.IsAny<HttpRequestMessage>(),ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage,CancellationToken>((message,_)=>body=message.Content!.ReadAsStringAsync().GetAwaiter().GetResult())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            Assert.IsType<OkObjectResult>(await _controller.Generate(request,CancellationToken.None));
            Assert.Contains("SELECTED-AI",body);
            Assert.Equal(plan.Id,(await _dbContext.LandSubmissions.SingleAsync()).BasePreDesignedPlanId);
            Assert.Empty(_dbContext.HouseDesigns);
        }
    }
}
