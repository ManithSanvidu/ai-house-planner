using System.Net;
using System.Text.Json;
using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace HousePlanner.API.Tests.Controllers;

public class RevisionBathroomTests
{
    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    public async Task Revision_ForwardsBathroomRequirementAndSavedPreferences(bool savedRequirements, int actualBathrooms)
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var submission = new LandSubmission
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            LandSizePerches = 20,
            PreferredBedrooms = 3,
            PreferredFloors = 2,
            StylePreference = "modern"
        };
        var layout = new Dictionary<string, object?>
        {
            ["floor_count"] = 2,
            ["rooms"] = Enumerable.Range(1, actualBathrooms).Select(index => new { room_type = $"bathroom_{index}" }).ToArray()
        };
        if (savedRequirements)
            layout["candidate_summary"] = new
            {
                normalized_input = new
                {
                    bedrooms = 3,
                    bathrooms = 2,
                    floors = 2,
                    architectural_style = "conventional",
                    home_office = true,
                    utility_room = true,
                    open_plan = false
                }
            };
        var workflow = new WorkflowState
        {
            Id = Guid.NewGuid(),
            LandSubmissionId = submission.Id,
            LandSubmission = submission,
            TerrainType = "flat",
            HouseDesigns = new List<HouseDesign>
            {
                new() { Id = Guid.NewGuid(), Version = 1, FloorCount = 2, FoundationType = "slab",
                        LayoutJson = JsonSerializer.Serialize(layout) }
            }
        };
        db.WorkflowStates.Add(workflow);
        await db.SaveChangesAsync();
        using var handler = new CaptureHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://agentic.test") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var workflowService = new Mock<IWorkflowService>();
        workflowService.Setup(service => service.ProcessApprovalAsync(
            workflow.Id, It.IsAny<ApprovalRequestDto>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(ApprovalServiceResult.Success(new ApprovalResponseDto
            {
                WorkflowId = workflow.Id,
                Decision = "request_revision",
                Status = "revision_requested"
            }));
        var currentUser = new Mock<ICurrentUserContextService>();
        currentUser.Setup(x => x.GetAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new CurrentUserContext(submission.ClientId, "customer@example.com", "Customer"));
        var controller = new WorkflowController(db, Mock.Of<ILogger<WorkflowController>>(), factory.Object,
            workflowService.Object, currentUser.Object);

        var result = await controller.ApproveWorkflow(workflow.Id,
            new ApprovalRequestDto("request_revision", "make living room bigger"));

        Assert.IsType<OkObjectResult>(result);
        using var payload = JsonDocument.Parse(handler.Body!);
        var preferences = payload.RootElement.GetProperty("preferences");
        Assert.Equal(2, preferences.GetProperty("bathrooms").GetInt32());
        Assert.Equal(3, preferences.GetProperty("bedrooms").GetInt32());
        Assert.Equal(2, preferences.GetProperty("floors").GetInt32());
        if (savedRequirements)
        {
            Assert.Equal("conventional", preferences.GetProperty("style").GetString());
            Assert.True(preferences.GetProperty("home_office").GetBoolean());
            Assert.True(preferences.GetProperty("utility_room").GetBoolean());
            Assert.False(preferences.GetProperty("open_plan").GetBoolean());
        }
    }
}
