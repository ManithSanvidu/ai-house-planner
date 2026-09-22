import re

with open("HousePlanner.API.Tests/Controllers/WorkflowControllerTests.cs", "r") as f:
    text = f.read()

# Replace SubmitArchitectReview_RequiresSelectedDesign
text = re.sub(
    r"public async Task SubmitArchitectReview_RequiresSelectedDesign\(\)\s*\{\s*var workflowId = Guid.NewGuid\(\);\s*_dbContext.WorkflowStates.Add\(new WorkflowState \{ Id = workflowId, LandSubmissionId = Guid.NewGuid\(\), Status = \"design_generated\" \}\);\s*await _dbContext.SaveChangesAsync\(\);\s*Assert.IsType<BadRequestObjectResult>\(await _controller.SubmitArchitectReview\(workflowId\)\);\s*\}",
    """public async Task SubmitArchitectReview_ValidatesDesignExists()
    {
        var workflowId = Guid.NewGuid();
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "design_generated" });
        await _dbContext.SaveChangesAsync();

        Assert.IsType<NotFoundObjectResult>(await _controller.SubmitArchitectReview(workflowId, Guid.NewGuid()));
    }""",
    text
)

# Replace SubmitArchitectReview_CreatesValidationRequest
text = re.sub(
    r"public async Task SubmitArchitectReview_CreatesValidationRequest\(\)\s*\{\s*var workflowId = Guid.NewGuid\(\); var design = Design\(workflowId, 1, false\);\s*_dbContext.WorkflowStates.Add\(new WorkflowState \{ Id = workflowId, LandSubmissionId = Guid.NewGuid\(\), Status = \"design_generated\", HouseDesigns = \[design\], PreferredHouseDesignId = design.Id \}\);\s*await _dbContext.SaveChangesAsync\(\);\s*Assert.IsType<OkObjectResult>\(await _controller.SubmitArchitectReview\(workflowId\)\);\s*var req = await _dbContext.ValidationRequests.SingleAsync\(\);\s*Assert.Equal\(workflowId, req.WorkflowStateId\);\s*Assert.Equal\(design.Id, req.HouseDesignId\);\s*Assert.Equal\(\"awaiting_architect_review\", \(await _dbContext.WorkflowStates.FindAsync\(workflowId\)\)!.Status\);\s*\}",
    """public async Task SubmitArchitectReview_CreatesValidationRequest()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, false);
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "design_generated", HouseDesigns = [design] });
        await _dbContext.SaveChangesAsync();

        Assert.IsType<OkObjectResult>(await _controller.SubmitArchitectReview(workflowId, design.Id));
        var req = await _dbContext.ValidationRequests.SingleAsync();
        Assert.Equal(workflowId, req.WorkflowStateId);
        Assert.Equal(design.Id, req.HouseDesignId);
        Assert.Equal("awaiting_architect_review", (await _dbContext.WorkflowStates.FindAsync(workflowId))!.Status);
    }""",
    text
)

# Replace SubmitArchitectReview_PreventsDuplicateRequests
text = re.sub(
    r"public async Task SubmitArchitectReview_PreventsDuplicateRequests\(\)\s*\{\s*var workflowId = Guid.NewGuid\(\); var design = Design\(workflowId, 1, false\);\s*_dbContext.WorkflowStates.Add\(new WorkflowState \{ Id = workflowId, LandSubmissionId = Guid.NewGuid\(\), Status = \"awaiting_architect_review\", HouseDesigns = \[design\], PreferredHouseDesignId = design.Id \}\);\s*_dbContext.ValidationRequests.Add\(new ValidationRequest \{ WorkflowStateId = workflowId, HouseDesignId = design.Id, Status = \"Pending\" \}\);\s*await _dbContext.SaveChangesAsync\(\);\s*var res = Assert.IsType<ConflictObjectResult>\(await _controller.SubmitArchitectReview\(workflowId\)\);\s*Assert.Contains\(\"active architect review\", res.Value!.ToString\(\)\);\s*\}",
    """public async Task SubmitArchitectReview_PreventsDuplicateRequests()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, false);
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "awaiting_architect_review", HouseDesigns = [design] });
        _dbContext.ValidationRequests.Add(new ValidationRequest { WorkflowStateId = workflowId, HouseDesignId = design.Id, Status = "Pending" });
        await _dbContext.SaveChangesAsync();

        var res = Assert.IsType<ConflictObjectResult>(await _controller.SubmitArchitectReview(workflowId, design.Id));
        Assert.Contains("active architect review", res.Value!.ToString());
    }
    
    [Fact]
    public async Task RegenerateDesign_InvokesAgenticServiceAndSetsStatusRunning()
    {
        var workflowId = Guid.NewGuid();
        var design = Design(workflowId, 1, true);
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "design_generated", HouseDesigns = [design] });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.RegenerateDesign(workflowId, design.Id);
        Assert.IsType<OkObjectResult>(result);

        var persisted = await _dbContext.WorkflowStates.FindAsync(workflowId);
        Assert.Equal("running", persisted!.Status);
    }""",
    text
)

with open("HousePlanner.API.Tests/Controllers/WorkflowControllerTests.cs", "w") as f:
    f.write(text)

with open("HousePlanner.API.Tests/Controllers/CustomerOwnershipTests.cs", "r") as f:
    text2 = f.read()

text2 = re.sub(
    r"await _controller.SubmitArchitectReview\(workflowId\)",
    r"await _controller.SubmitArchitectReview(workflowId, Guid.NewGuid())",
    text2
)

with open("HousePlanner.API.Tests/Controllers/CustomerOwnershipTests.cs", "w") as f:
    f.write(text2)
