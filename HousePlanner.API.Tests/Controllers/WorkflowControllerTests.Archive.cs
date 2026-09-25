using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Moq;
using HousePlanner.API.Services;

namespace HousePlanner.API.Tests.Controllers
{
    public partial class WorkflowControllerTests
    {
        [Fact]
        public async Task CancelledConstructionProject_DoesNotBlockDesignArchive()
        {
            var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
            _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = _clientId, Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
            _dbContext.Projects.Add(new Project { Id = Guid.NewGuid(), WorkflowStateId = workflowId, HouseDesignId = design.Id, ContractorId = Guid.NewGuid(), Status = "Cancelled" });
            await _dbContext.SaveChangesAsync();

            var result = Assert.IsType<OkObjectResult>(await _controller.RemoveDesign(workflowId, design.Id));
            Assert.True((await _dbContext.HouseDesigns.FindAsync(design.Id))!.IsArchived);
        }

        [Fact]
        public async Task CompletedProject_DoesNotCountAsActiveConstruction()
        {
            var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
            _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = _clientId, Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
            _dbContext.Projects.Add(new Project { Id = Guid.NewGuid(), WorkflowStateId = workflowId, HouseDesignId = design.Id, ContractorId = Guid.NewGuid(), Status = "completed" });
            await _dbContext.SaveChangesAsync();

            var result = Assert.IsType<OkObjectResult>(await _controller.RemoveDesign(workflowId, design.Id));
            Assert.True((await _dbContext.HouseDesigns.FindAsync(design.Id))!.IsArchived);
        }

        [Fact]
        public async Task CancelledProject_RemainsInDatabaseAfterDesignArchive()
        {
            var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
            var projectId = Guid.NewGuid();
            _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = _clientId, Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
            _dbContext.Projects.Add(new Project { Id = projectId, WorkflowStateId = workflowId, HouseDesignId = design.Id, ContractorId = Guid.NewGuid(), Status = "Cancelled" });
            await _dbContext.SaveChangesAsync();

            await _controller.RemoveDesign(workflowId, design.Id);
            var project = await _dbContext.Projects.FindAsync(projectId);
            Assert.NotNull(project);
            Assert.Equal("Cancelled", project.Status);
        }

        [Fact]
        public async Task CancelledProjectHistory_IsPreserved()
        {
            var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
            var projectId = Guid.NewGuid();
            _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = _clientId, Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
            _dbContext.Projects.Add(new Project
            {
                Id = projectId,
                WorkflowStateId = workflowId,
                HouseDesignId = design.Id,
                ContractorId = Guid.NewGuid(),
                Status = "Cancelled",
                ConstructionPhases = [new ConstructionPhase { Id = Guid.NewGuid(), PhaseName = "Phase 1" }]
            });
            await _dbContext.SaveChangesAsync();

            await _controller.RemoveDesign(workflowId, design.Id);
            var project = await _dbContext.Projects.Include(p => p.ConstructionPhases).FirstOrDefaultAsync(p => p.Id == projectId);
            Assert.NotNull(project);
            Assert.NotEmpty(project.ConstructionPhases);
        }
    }
}
