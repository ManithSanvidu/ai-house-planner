import re
with open('HousePlanner.API.Tests/Controllers/WorkflowControllerTests.cs', 'r') as f:
    content = f.read()

# Let's just remove the tests I added in the previous session and add the new ones.
# Find the start of Customer_CanArchiveApprovedSelectedDesign
start_idx = content.find('public async Task Customer_CanArchiveApprovedSelectedDesign()')
if start_idx != -1:
    # go back to the previous [Fact]
    start_idx = content.rfind('[Fact]', 0, start_idx)
    content = content[:start_idx]

new_tests = """
    [Fact]
    public async Task ApprovedDesign_NoConstructorRequest_CanArchive()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
        await _dbContext.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await _controller.RemoveDesign(workflowId, design.Id));
        Assert.True((await _dbContext.HouseDesigns.FindAsync(design.Id))!.IsArchived);
    }

    [Fact]
    public async Task ApprovedDesign_PendingConstructorRequest_CanArchive()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
        var subId = Guid.NewGuid();
        _dbContext.LandSubmissions.Add(new LandSubmission { Id = subId, ClientId = _clientId, LandSizePerches = 10 });
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = subId, Status = "approved", HouseDesigns = [design] });
        var req = new ConstructorProjectRequest { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid(), CustomerId = _clientId, ConstructorId = Guid.NewGuid(), HouseDesignId = design.Id, Status = "Pending" };
        _dbContext.ConstructorProjectRequests.Add(req);
        await _dbContext.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await _controller.RemoveDesign(workflowId, design.Id));
        Assert.True((await _dbContext.HouseDesigns.FindAsync(design.Id))!.IsArchived);
    }

    [Fact]
    public async Task ArchivingDesign_CancelsPendingConstructorRequest()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
        var subId = Guid.NewGuid();
        _dbContext.LandSubmissions.Add(new LandSubmission { Id = subId, ClientId = _clientId, LandSizePerches = 10 });
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = subId, Status = "approved", HouseDesigns = [design] });
        var req = new ConstructorProjectRequest { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid(), CustomerId = _clientId, ConstructorId = Guid.NewGuid(), HouseDesignId = design.Id, Status = "Pending" };
        _dbContext.ConstructorProjectRequests.Add(req);
        await _dbContext.SaveChangesAsync();

        await _controller.RemoveDesign(workflowId, design.Id);
        var dbReq = await _dbContext.ConstructorProjectRequests.FindAsync(req.Id);
        Assert.Equal("Cancelled", dbReq!.Status);
    }

    [Fact]
    public async Task ApprovedSelectedDesign_ArchiveClearsPreferredHouseDesignId()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
        await _dbContext.SaveChangesAsync();

        await _controller.RemoveDesign(workflowId, design.Id);
        Assert.Null((await _dbContext.WorkflowStates.FindAsync(workflowId))!.PreferredHouseDesignId);
    }

    [Fact]
    public async Task ApprovedDesign_ActiveProject_Returns409()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "approved", HouseDesigns = [design] });
        _dbContext.Projects.Add(new Project { Id = Guid.NewGuid(), WorkflowStateId = workflowId, HouseDesignId = design.Id, ContractorId = Guid.NewGuid() });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.RemoveDesign(workflowId, design.Id);
        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var value = conflict.Value as dynamic;
        Assert.Equal("design_in_active_construction", (string)value.GetType().GetProperty("code").GetValue(value, null));
    }

    [Fact]
    public async Task ApprovedDesign_ValidationHistoryPreserved()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
        _dbContext.ValidationRequests.Add(new ValidationRequest { Id = Guid.NewGuid(), WorkflowStateId = workflowId, HouseDesignId = design.Id, Status = "Approved" });
        await _dbContext.SaveChangesAsync();

        await _controller.RemoveDesign(workflowId, design.Id);
        Assert.True((await _dbContext.HouseDesigns.FindAsync(design.Id))!.IsArchived);
        Assert.Single(await _dbContext.ValidationRequests.Where(r => r.HouseDesignId == design.Id).ToListAsync());
    }

    [Fact]
    public async Task ArchivedDesign_NotReturnedByMyDesigns()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
        var subId = Guid.NewGuid();
        _dbContext.LandSubmissions.Add(new LandSubmission { Id = subId, ClientId = _clientId, LandSizePerches = 10 });
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = subId, Status = "approved", HouseDesigns = [design] });
        await _dbContext.SaveChangesAsync();

        await _controller.RemoveDesign(workflowId, design.Id);
        
        var result = Assert.IsType<OkObjectResult>(await _controller.GetMyDesigns());
        var workflows = Assert.IsAssignableFrom<IEnumerable<WorkflowDesignHistoryDto>>(result.Value);
        Assert.Empty(workflows);
    }

    [Fact]
    public async Task CustomerCannotArchiveAnotherCustomersDesign()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
        var subId = Guid.NewGuid();
        _dbContext.LandSubmissions.Add(new LandSubmission { Id = subId, ClientId = Guid.NewGuid(), LandSizePerches = 10 });
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = subId, Status = "approved", HouseDesigns = [design] });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.RemoveDesign(workflowId, design.Id);
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
"""
if not content.endswith('}\n'):
    if content.endswith('}'):
        content = content[:-1]
    content = content.rstrip()
    if content.endswith('}'):
        content = content[:-1]

with open('HousePlanner.API.Tests/Controllers/WorkflowControllerTests.cs', 'w') as f:
    f.write(content + new_tests)
