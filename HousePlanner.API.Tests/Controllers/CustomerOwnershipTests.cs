using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HousePlanner.API.Tests.Controllers;

public sealed class CustomerOwnershipTests
{
    private readonly ApplicationDbContext _db;
    private readonly Guid _customerA = Guid.NewGuid();
    private readonly Guid _customerB = Guid.NewGuid();
    private readonly WorkflowState _workflowA;
    private readonly WorkflowState _workflowB;
    private readonly Mock<IWorkflowService> _workflowService = new();

    public CustomerOwnershipTests()
    {
        _db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var customerRole = new Role { Id = 1, Name = "Customer" };
        _db.Roles.Add(customerRole);
        _db.Users.AddRange(
            new User { Id = _customerA, Email = "a@example.com", FullName = "Customer A", RoleId = customerRole.Id },
            new User { Id = _customerB, Email = "b@example.com", FullName = "Customer B", RoleId = customerRole.Id });
        _workflowA = Workflow(_customerA);
        _workflowB = Workflow(_customerB);
        _db.WorkflowStates.AddRange(_workflowA, _workflowB);
        _db.SaveChanges();
    }

    private static WorkflowState Workflow(Guid owner)
    {
        var submission = new LandSubmission
        {
            Id = Guid.NewGuid(),
            ClientId = owner,
            LandSizePerches = 12,
            PreferredBedrooms = 3,
            PreferredFloors = 1
        };
        var workflow = new WorkflowState
        {
            Id = Guid.NewGuid(),
            LandSubmissionId = submission.Id,
            LandSubmission = submission,
            Status = "design_generated",
            ApprovalStatus = "client_review"
        };
        workflow.HouseDesigns.Add(new HouseDesign
        {
            Id = Guid.NewGuid(),
            WorkflowStateId = workflow.Id,
            Version = 1,
            IsCurrent = true,
            FloorCount = 1,
            TotalBuiltUpAreaSqft = 800,
            FoundationType = "slab",
            LayoutJson = "{\"rooms\":[]}"
        });
        return workflow;
    }

    private WorkflowController Controller(Guid? userId = null)
    {
        var current = new Mock<ICurrentUserContextService>();
        current.Setup(x => x.GetAsync(It.IsAny<HttpContext>())).ReturnsAsync(userId is null
            ? null : new CurrentUserContext(userId, "customer@example.com", "Customer"));
        var clients = new Mock<IHttpClientFactory>();
        clients.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        return new WorkflowController(_db, NullLogger<WorkflowController>.Instance, clients.Object,
            _workflowService.Object, current.Object);
    }

    [Fact]
    public async Task Customer_CanReadOwnWorkflowAndDesigns()
    {
        Assert.IsType<OkObjectResult>((await Controller(_customerA).GetWorkflowStatus(_workflowA.Id)).Result);
        Assert.IsType<OkObjectResult>(await Controller(_customerA).GetDesigns(_workflowA.Id));
    }

    [Fact]
    public async Task Customer_CannotReadAnotherCustomersWorkflowOrDesigns()
    {
        Assert.IsType<NotFoundObjectResult>((await Controller(_customerA).GetWorkflowStatus(_workflowB.Id)).Result);
        Assert.IsType<NotFoundObjectResult>(await Controller(_customerA).GetDesigns(_workflowB.Id));
    }

    [Fact]
    public async Task Customer_CannotSelectOrUnselectAnotherCustomersDesign()
    {
        var controller = Controller(_customerA);
        Assert.IsType<NotFoundObjectResult>(await controller.SelectDesign(_workflowB.Id, _workflowB.HouseDesigns.Single().Id));
        Assert.IsType<NotFoundObjectResult>(await controller.ClearDesignSelection(_workflowB.Id));
    }

    [Fact]
    public async Task Customer_CannotArchiveAnotherCustomersDesign()
    {
        Assert.IsType<NotFoundObjectResult>(await Controller(_customerA)
            .RemoveDesign(_workflowB.Id, _workflowB.HouseDesigns.Single().Id));
        Assert.False(_workflowB.HouseDesigns.Single().IsArchived);
    }

    [Fact]
    public async Task Customer_CannotUseForeignDesignIdInsideOwnedWorkflow()
    {
        Assert.IsType<BadRequestObjectResult>(await Controller(_customerA)
            .SelectDesign(_workflowA.Id, _workflowB.HouseDesigns.Single().Id));
    }

    [Fact]
    public async Task Customer_CannotGenerateAnotherOrReviseAnotherCustomersWorkflow()
    {
        var result = await Controller(_customerA).ApproveWorkflow(_workflowB.Id,
            new ApprovalRequestDto("request_revision", "Generate Another"));
        Assert.IsType<NotFoundObjectResult>(result);
        _workflowService.Verify(x => x.ProcessApprovalAsync(It.IsAny<Guid>(), It.IsAny<ApprovalRequestDto>(),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Customer_CannotSubmitAnotherCustomersDesignForArchitectReview()
    {
        _workflowB.PreferredHouseDesignId = _workflowB.HouseDesigns.Single().Id;
        Assert.IsType<NotFoundObjectResult>(await Controller(_customerA).SubmitArchitectReview(_workflowB.Id, _workflowB.HouseDesigns.Single().Id));
        Assert.Empty(_db.ValidationRequests);
    }

    [Fact]
    public async Task Customer_CannotCreateValidationRequestForAnotherCustomersWorkflow()
    {
        var current = new Mock<ICurrentUserContextService>();
        current.Setup(x => x.GetAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new CurrentUserContext(_customerA, "a@example.com", "Customer"));
        var controller = new ValidationRequestController(_db, current.Object)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        Assert.IsType<NotFoundObjectResult>(await controller.CreateValidationRequest(
            new CreateValidationRequestDto { WorkflowStateId = _workflowB.Id }));
    }

    [Fact]
    public async Task Architect_CannotActOnWorkflowThatWasNotSubmittedForReview()
    {
        var current = new Mock<ICurrentUserContextService>();
        current.Setup(x => x.GetAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new CurrentUserContext(Guid.NewGuid(), "architect@example.com", "Architect"));
        var clients = new Mock<IHttpClientFactory>();
        clients.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        var controller = new WorkflowController(_db, NullLogger<WorkflowController>.Instance, clients.Object,
            _workflowService.Object, current.Object);

        Assert.IsType<NotFoundObjectResult>(await controller.ApproveWorkflow(_workflowA.Id,
            new ApprovalRequestDto("approve", null)));
        _workflowService.Verify(x => x.ProcessApprovalAsync(It.IsAny<Guid>(), It.IsAny<ApprovalRequestDto>(),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task UnauthenticatedCustomerResourceAccessReturns401()
    {
        Assert.IsType<UnauthorizedResult>((await Controller().GetWorkflowStatus(_workflowA.Id)).Result);
        Assert.IsType<UnauthorizedResult>(await Controller().GetDesigns(_workflowA.Id));
    }
}
