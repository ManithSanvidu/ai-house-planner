using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;

namespace HousePlanner.API.Tests.Controllers;

public sealed class ValidationRequestLifecycleTests
{
    [Theory]
    [InlineData("Customer")]
    [InlineData("Constructor")]
    [InlineData("Admin")]
    public async Task NonArchitectRolesCannotAccessArchitectValidationEndpoints(string role)
    {
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var current = new Mock<ICurrentUserContextService>();
        current.Setup(x => x.GetAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new CurrentUserContext(Guid.NewGuid(), "user@example.com", role));
        var controller = new ArchitectValidationRequestsController(db, current.Object)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        Assert.IsType<ForbidResult>(await controller.GetSummary());
    }

    private static async Task<(ApplicationDbContext Db, ArchitectValidationRequestsController Controller, ValidationRequest Request)> Setup(bool includeCost = true)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var customer = Guid.NewGuid(); var architect = Guid.NewGuid(); var workflowId = Guid.NewGuid();
        var customerRole = new Role { Id = 1, Name = "Customer" };
        var architectRole = new Role { Id = 2, Name = "Architect" };
        var customerUser = new User { Id = customer, Email = "customer@example.com", FullName = "Customer", RoleId = customerRole.Id, Role = customerRole };
        var architectUser = new User { Id = architect, Email = "architect@example.com", FullName = "Architect", RoleId = architectRole.Id, Role = architectRole };
        var land = new LandSubmission { Id = Guid.NewGuid(), ClientId = customer, LandSizePerches = 10, PreferredBedrooms = 3, PreferredFloors = 1, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        var design = new HouseDesign { Id = Guid.NewGuid(), WorkflowStateId = workflowId, Version = 2, FloorCount = 1, TotalBuiltUpAreaSqft = 900, FoundationType = "slab", LayoutJson = "{}", IsCurrent = true };
        var workflow = new WorkflowState { Id = workflowId, LandSubmissionId = land.Id, LandSubmission = land, Status = "awaiting_architect_review", ApprovalStatus = "awaiting_architect_review", PreferredHouseDesignId = design.Id, HouseDesigns = [design] };
        var request = new ValidationRequest { Id = Guid.NewGuid(), WorkflowStateId = workflowId, WorkflowState = workflow, HouseDesignId = design.Id, HouseDesign = design, ClientId = customer, Status = "Pending" };
        db.AddRange(customerRole, architectRole, customerUser, architectUser, land, workflow, request);
        await db.SaveChangesAsync();
        if (includeCost) db.CostEstimates.Add(new CostEstimate
        {
            HouseDesignId = design.Id,
            MaterialCostLkr = 8_400_000,
            LabourCostLkr = 2_940_000,
            TotalCostLkr = 11_340_000,
            BudgetDeltaPercent = 94.50m
        });
        if (includeCost) await db.SaveChangesAsync();
        var current = new Mock<ICurrentUserContextService>(); current.Setup(x => x.GetAsync(It.IsAny<HttpContext>())).ReturnsAsync(new CurrentUserContext(architect, "architect@example.com", "Architect"));
        var controller = new ArchitectValidationRequestsController(db, current.Object) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        return (db, controller, request);
    }

    [Fact]
    public async Task RejectRequiresReasonAndFinalizedRequestCannotChangeDecision()
    { var (db, c, r) = await Setup(); Assert.IsType<BadRequestObjectResult>(await c.RequestRevision(r.Id, new ArchitectReviewDto())); Assert.IsType<OkObjectResult>(await c.RequestRevision(r.Id, new ArchitectReviewDto("Circulation needs improvement."))); Assert.Equal("revision_requested", (await db.WorkflowStates.SingleAsync()).Status); Assert.IsType<ConflictObjectResult>(await c.Approve(r.Id, new ArchitectReviewDto())); }

    [Fact]
    public async Task ApprovedRequestCannotBeApprovedOrRejectedAgain()
    { var (_, c, r) = await Setup(); Assert.IsType<OkObjectResult>(await c.Approve(r.Id, new ArchitectReviewDto("Approved"))); Assert.IsType<ConflictObjectResult>(await c.Approve(r.Id, new ArchitectReviewDto())); Assert.IsType<ConflictObjectResult>(await c.Reject(r.Id, new ArchitectReviewDto("Changed mind"))); }

    [Fact]
    public async Task ArchitectDetailsIncludeCostForTheSubmittedDesign()
    {
        var (db, controller, request) = await Setup();
        Assert.True(await db.ValidationRequests.AnyAsync(v => v.Id == request.Id));

        var result = Assert.IsType<OkObjectResult>(await controller.GetRequestDetails(request.Id));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        var cost = json.RootElement.GetProperty("cost");
        Assert.Equal(11_340_000m, cost.GetProperty("totalCostLkr").GetDecimal());
        Assert.Equal(94.50m, cost.GetProperty("budgetDeltaPercent").GetDecimal());
        Assert.True(json.RootElement.GetProperty("approvalEligibility").GetProperty("canApprove").GetBoolean());
    }

    [Fact]
    public async Task ArchitectCanApproveWithoutCostEstimate()
    {
        var (_, controller, request) = await Setup(includeCost: false);
        var details = Assert.IsType<OkObjectResult>(await controller.GetRequestDetails(request.Id));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(details.Value));
        Assert.True(json.RootElement.GetProperty("approvalEligibility").GetProperty("canApprove").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("approvalEligibility").GetProperty("reason").ValueKind);

        Assert.IsType<OkObjectResult>(await controller.Approve(request.Id, new ArchitectReviewDto("Approved without estimate.")));
    }
}
