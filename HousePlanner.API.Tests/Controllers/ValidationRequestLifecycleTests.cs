using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HousePlanner.API.Tests.Controllers;

public sealed class ValidationRequestLifecycleTests
{
    private static async Task<(ApplicationDbContext Db, ValidationRequestController Controller, ValidationRequest Request)> Setup()
    {
        var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var customer=Guid.NewGuid();var architect=Guid.NewGuid();var workflowId=Guid.NewGuid();
        var land=new LandSubmission{Id=Guid.NewGuid(),ClientId=customer,LandSizePerches=10,PreferredBedrooms=3,PreferredFloors=1,CreatedAt=DateTimeOffset.UtcNow,UpdatedAt=DateTimeOffset.UtcNow};
        var design=new HouseDesign{Id=Guid.NewGuid(),WorkflowStateId=workflowId,Version=2,FloorCount=1,TotalBuiltUpAreaSqft=900,FoundationType="slab",LayoutJson="{}",IsCurrent=true};
        var workflow=new WorkflowState{Id=workflowId,LandSubmissionId=land.Id,LandSubmission=land,Status="awaiting_architect_review",ApprovalStatus="awaiting_architect_review",PreferredHouseDesignId=design.Id,HouseDesigns=[design]};
        var request=new ValidationRequest{Id=Guid.NewGuid(),WorkflowStateId=workflowId,WorkflowState=workflow,HouseDesignId=design.Id,HouseDesign=design,ClientId=customer,Status="Pending"};
        db.AddRange(land,workflow,request);await db.SaveChangesAsync();
        var current=new Mock<ICurrentUserContextService>();current.Setup(x=>x.GetAsync(It.IsAny<HttpContext>())).ReturnsAsync(new CurrentUserContext(architect,"architect@example.com","Architect"));
        var controller=new ValidationRequestController(db,current.Object){ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext()}};
        return(db,controller,request);
    }

    [Fact] public async Task RejectRequiresReasonAndFinalizedRequestCannotChangeDecision()
    {var (db,c,r)=await Setup();Assert.IsType<BadRequestObjectResult>(await c.RejectRequest(r.Id,new ArchitectReviewDto()));Assert.IsType<OkObjectResult>(await c.RejectRequest(r.Id,new ArchitectReviewDto{Review="Circulation needs improvement."}));Assert.Equal("revision_requested",(await db.WorkflowStates.SingleAsync()).Status);Assert.IsType<ConflictObjectResult>(await c.ApproveRequest(r.Id,new ArchitectReviewDto()));}

    [Fact] public async Task ApprovedRequestCannotBeApprovedOrRejectedAgain()
    {var (_,c,r)=await Setup();Assert.IsType<OkObjectResult>(await c.ApproveRequest(r.Id,new ArchitectReviewDto{Review="Approved"}));Assert.IsType<ConflictObjectResult>(await c.ApproveRequest(r.Id,new ArchitectReviewDto()));Assert.IsType<ConflictObjectResult>(await c.RejectRequest(r.Id,new ArchitectReviewDto{Review="Changed mind"}));}
}
