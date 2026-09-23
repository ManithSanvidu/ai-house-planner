using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HousePlanner.API.Tests.Controllers
{
    public partial class CustomerConstructionLifecycleTests
    {
        [Fact]
        public async Task ArchivedApprovedDesign_NotCountedOnOverview()
        {
            var workflowId = Guid.NewGuid();
            var design = new HouseDesign { Id = Guid.NewGuid(), WorkflowStateId = workflowId, Version = 1, DesignSource = "test", FloorCount = 1, TotalBuiltUpAreaSqft = 100, LayoutJson = "{}", IsCurrent = true, IsArchived = true };
            _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = _clientId, Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
            _dbContext.ValidationRequests.Add(new ValidationRequest { Id = Guid.NewGuid(), WorkflowStateId = workflowId, HouseDesignId = design.Id, ClientId = _clientId, Status = "Approved", DecisionAt = DateTimeOffset.UtcNow });
            await _dbContext.SaveChangesAsync();

            var result = Assert.IsType<OkObjectResult>(await _controller.ApprovedDesigns());
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            Assert.Equal(0, doc.RootElement.GetArrayLength());
        }

        [Fact]
        public async Task NonArchivedApprovedDesign_IsShownOnOverview()
        {
            var workflowId = Guid.NewGuid();
            var design = new HouseDesign { Id = Guid.NewGuid(), WorkflowStateId = workflowId, Version = 1, DesignSource = "test", FloorCount = 1, TotalBuiltUpAreaSqft = 100, LayoutJson = "{}", IsCurrent = true, IsArchived = false };
            _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = _clientId, Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
            _dbContext.ValidationRequests.Add(new ValidationRequest { Id = Guid.NewGuid(), WorkflowStateId = workflowId, HouseDesignId = design.Id, ClientId = _clientId, Status = "Approved", DecisionAt = DateTimeOffset.UtcNow });
            await _dbContext.SaveChangesAsync();

            var result = Assert.IsType<OkObjectResult>(await _controller.ApprovedDesigns());
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            Assert.Equal(1, doc.RootElement.GetArrayLength());
        }

        [Fact]
        public async Task DuplicateValidationRequests_CountAsOneApprovedDesign()
        {
            var workflowId = Guid.NewGuid();
            var design = new HouseDesign { Id = Guid.NewGuid(), WorkflowStateId = workflowId, Version = 1, DesignSource = "test", FloorCount = 1, TotalBuiltUpAreaSqft = 100, LayoutJson = "{}", IsCurrent = true, IsArchived = false };
            _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = _clientId, Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
            _dbContext.ValidationRequests.Add(new ValidationRequest { Id = Guid.NewGuid(), WorkflowStateId = workflowId, HouseDesignId = design.Id, ClientId = _clientId, Status = "Approved", DecisionAt = DateTimeOffset.UtcNow.AddMinutes(-5) });
            _dbContext.ValidationRequests.Add(new ValidationRequest { Id = Guid.NewGuid(), WorkflowStateId = workflowId, HouseDesignId = design.Id, ClientId = _clientId, Status = "Approved", DecisionAt = DateTimeOffset.UtcNow });
            await _dbContext.SaveChangesAsync();

            var result = Assert.IsType<OkObjectResult>(await _controller.ApprovedDesigns());
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            Assert.Equal(1, doc.RootElement.GetArrayLength());
        }

        [Fact]
        public async Task ApprovalHistory_RemainsAfterArchive()
        {
            var workflowId = Guid.NewGuid();
            var design = new HouseDesign { Id = Guid.NewGuid(), WorkflowStateId = workflowId, Version = 1, DesignSource = "test", FloorCount = 1, TotalBuiltUpAreaSqft = 100, LayoutJson = "{}", IsCurrent = true, IsArchived = true };
            _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = _clientId, Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
            var valId = Guid.NewGuid();
            _dbContext.ValidationRequests.Add(new ValidationRequest { Id = valId, WorkflowStateId = workflowId, HouseDesignId = design.Id, ClientId = _clientId, Status = "Approved", DecisionAt = DateTimeOffset.UtcNow });
            await _dbContext.SaveChangesAsync();

            var result = Assert.IsType<OkObjectResult>(await _controller.ApprovedDesigns());
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            Assert.Equal(0, doc.RootElement.GetArrayLength());

            var valHistory = await _dbContext.ValidationRequests.FindAsync(valId);
            Assert.NotNull(valHistory);
            Assert.Equal("Approved", valHistory.Status);
        }
    }
}
