using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HousePlanner.API.Tests.Controllers;

public sealed class CustomerConstructionLifecycleTests
{
    private readonly ApplicationDbContext _db;
    private readonly Guid _customerA = Guid.NewGuid();
    private readonly Guid _customerB = Guid.NewGuid();
    private readonly Guid _constructorA = Guid.NewGuid();
    private readonly Guid _constructorB = Guid.NewGuid();
    private readonly HouseDesign _approvedDesign;
    private readonly HouseDesign _foreignDesign;

    public CustomerConstructionLifecycleTests()
    {
        _db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var customerRole = new Role { Id = 1, Name = "Customer" };
        var constructorRole = new Role { Id = 2, Name = "Constructor" };
        _db.AddRange(customerRole, constructorRole,
            User(_customerA, "Customer A", customerRole), User(_customerB, "Customer B", customerRole),
            User(_constructorA, "Builder A", constructorRole), User(_constructorB, "Builder B", constructorRole));
        _approvedDesign = AddApprovedWorkflow(_customerA, true);
        _foreignDesign = AddApprovedWorkflow(_customerB, true);
        _db.SaveChanges();
    }

    [Fact]
    public async Task CustomerCanRequestRealConstructorForOwnApprovedDesign()
    {
        var result = await CustomerController(_customerA).CreateRequest(new(_approvedDesign.Id, _constructorA), default);
        Assert.IsType<CreatedAtActionResult>(result);
        var request = Assert.Single(_db.ConstructorProjectRequests);
        Assert.Equal(_customerA, request.CustomerId);
        Assert.Equal(_approvedDesign.Id, request.HouseDesignId);
    }

    [Fact]
    public async Task UnapprovedAndForeignDesignsCannotBeRequested()
    {
        var unapproved = AddApprovedWorkflow(_customerA, false); await _db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await CustomerController(_customerA).CreateRequest(new(unapproved.Id, _constructorA), default));
        Assert.IsType<NotFoundResult>(await CustomerController(_customerA).CreateRequest(new(_foreignDesign.Id, _constructorA), default));
    }

    [Fact]
    public async Task NonConstructorAndDuplicatePendingRequestAreRejected()
    {
        Assert.IsType<BadRequestObjectResult>(await CustomerController(_customerA).CreateRequest(new(_approvedDesign.Id, _customerB), default));
        Assert.IsType<CreatedAtActionResult>(await CustomerController(_customerA).CreateRequest(new(_approvedDesign.Id, _constructorA), default));
        Assert.IsType<ConflictObjectResult>(await CustomerController(_customerA).CreateRequest(new(_approvedDesign.Id, _constructorA), default));
    }

    [Fact]
    public async Task ConstructorCanAcceptOnlyOwnRequest()
    {
        await CustomerController(_customerA).CreateRequest(new(_approvedDesign.Id, _constructorA), default);
        var request = Assert.Single(_db.ConstructorProjectRequests);
        Assert.IsType<NotFoundResult>(await ConstructorController(_constructorB).AcceptRequest(request.Id));
        Assert.IsType<OkObjectResult>(await ConstructorController(_constructorA).AcceptRequest(request.Id));
        Assert.Equal("Accepted", request.Status);
        Assert.Equal(_constructorA, request.Project!.ContractorId);
    }

    [Fact]
    public async Task ConstructorCanDeclineOnlyOwnRequest()
    {
        await CustomerController(_customerA).CreateRequest(new(_approvedDesign.Id, _constructorA), default);
        var request = Assert.Single(_db.ConstructorProjectRequests);
        Assert.IsType<NotFoundResult>(await ConstructorController(_constructorB).DeclineRequest(request.Id, new("No capacity")));
        Assert.IsType<OkObjectResult>(await ConstructorController(_constructorA).DeclineRequest(request.Id, new("No capacity")));
        Assert.Equal("Declined", request.Status);
    }

    [Fact]
    public async Task AssignedConstructorCanLogAndOtherConstructorCannot()
    {
        await CustomerController(_customerA).CreateRequest(new(_approvedDesign.Id, _constructorA), default);
        var request = Assert.Single(_db.ConstructorProjectRequests);
        await ConstructorController(_constructorA).AcceptRequest(request.Id);
        var service = new ConstructorWorkflowService(_db);
        await service.CreateWorkflowLogAsync(_constructorA, new ConstructorWorkflowLog { Id=Guid.NewGuid(), ProjectId=request.ProjectId, CompletedWork="Foundation work", ProgressPercentage=20 });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateWorkflowLogAsync(_constructorB, new ConstructorWorkflowLog { Id=Guid.NewGuid(), ProjectId=request.ProjectId, CompletedWork="Invalid" }));
    }

    [Fact]
    public async Task CustomerCanReadOnlyOwnConstructionProjectAndRealActivity()
    {
        await CustomerController(_customerA).CreateRequest(new(_approvedDesign.Id, _constructorA), default);
        var request = Assert.Single(_db.ConstructorProjectRequests);
        await ConstructorController(_constructorA).AcceptRequest(request.Id);
        var service = new ConstructorWorkflowService(_db);
        await service.CreateWorkflowLogAsync(_constructorA, new ConstructorWorkflowLog { Id=Guid.NewGuid(), ProjectId=request.ProjectId, CompletedWork="A", Date=DateTimeOffset.UtcNow });
        await service.CreateWorkflowLogAsync(_constructorA, new ConstructorWorkflowLog { Id=Guid.NewGuid(), ProjectId=request.ProjectId, CompletedWork="B", Date=DateTimeOffset.UtcNow });
        Assert.IsType<OkObjectResult>(await CustomerController(_customerA).Project(request.ProjectId));
        Assert.IsType<NotFoundResult>(await CustomerController(_customerB).Project(request.ProjectId));
    }

    private CustomerConstructionController CustomerController(Guid id)
    {
        var current = Current(id, "Customer");
        var staffMock = new Mock<IStaffAccountService>();
        staffMock.Setup(s => s.ListAsync("Constructor", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HousePlanner.API.DTOs.StaffAccountDto> {
                new(_constructorA, "Builder A", "a@example.com", "Constructor", "Active"),
                new(_constructorB, "Builder B", "b@example.com", "Constructor", "Active")
            });
        return new CustomerConstructionController(_db, current.Object, new ConstructorWorkflowService(_db), staffMock.Object)
            { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
    }
    private ConstructorWorkflowController ConstructorController(Guid id)
    {
        var current = Current(id, "Constructor");
        return new ConstructorWorkflowController(new ConstructorWorkflowService(_db), current.Object, _db)
            { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
    }
    private static Mock<ICurrentUserContextService> Current(Guid id, string role)
    {
        var current = new Mock<ICurrentUserContextService>();
        current.Setup(x => x.GetAsync(It.IsAny<HttpContext>())).ReturnsAsync(new CurrentUserContext(id, "x@example.com", role));
        return current;
    }
    private static User User(Guid id, string name, Role role) => new() { Id=id, Email=$"{id}@example.com", FullName=name, Role=role, RoleId=role.Id };
    private HouseDesign AddApprovedWorkflow(Guid owner, bool approved)
    {
        var land = new LandSubmission { Id=Guid.NewGuid(), ClientId=owner, LandSizePerches=10, PreferredBedrooms=3, PreferredFloors=1 };
        var workflow = new WorkflowState { Id=Guid.NewGuid(), LandSubmission=land, LandSubmissionId=land.Id, Status=approved?"approved":"design_generated", ApprovalStatus=approved?"approved":"not_requested" };
        var design = new HouseDesign { Id=Guid.NewGuid(), WorkflowState=workflow, WorkflowStateId=workflow.Id, Version=1, FloorCount=1, TotalBuiltUpAreaSqft=900, FoundationType="slab", LayoutJson="{\"rooms\":[]}" };
        workflow.HouseDesigns.Add(design); workflow.PreferredHouseDesignId=design.Id;
        _db.Add(workflow);
        if (approved) _db.Add(new ValidationRequest { Id=Guid.NewGuid(), WorkflowState=workflow, WorkflowStateId=workflow.Id, HouseDesign=design, HouseDesignId=design.Id, ClientId=owner, Status="Approved" });
        return design;
    }
}

