import sys

with open("HousePlanner.API.Tests/Controllers/WorkflowControllerTests.cs", "r") as f:
    content = f.read()

target = """    }
}
"""

replacement = """
    [Fact]
    public async Task GetWorkflowStatus_CannotBeReadByDifferentCustomer()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        
        // This creates a workflow owned by some other user (not _clientId)
        _dbContext.LandSubmissions.Add(new LandSubmission
        {
            Id = submissionId, ClientId = Guid.NewGuid(), LandSizePerches = 10,
            PreferredBedrooms = 3, PreferredFloors = 1
        });
        _dbContext.WorkflowStates.Add(new WorkflowState
        {
            Id = workflowId,
            LandSubmissionId = submissionId,
            Status = "pending",
            TerrainType = "flat"
        });
        await _dbContext.SaveChangesAsync();

        // Act - _controller is configured with _clientId as the current user
        var result = await _controller.GetWorkflowStatus(workflowId);

        // Assert - The controller returns 404 because the ownership check fails
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
}
"""

if target in content:
    content = content.replace(target, replacement)
    with open("HousePlanner.API.Tests/Controllers/WorkflowControllerTests.cs", "w") as f:
        f.write(content)
    print("Replaced successfully")
else:
    print("Target not found")
