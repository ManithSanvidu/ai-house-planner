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
using System.Linq;

namespace HousePlanner.API.Tests.Controllers
{
    public partial class CustomerConstructionLifecycleTests
    {
        [Fact]
        public async Task CancelledProject_NotReturnedInCustomerActiveConstruction()
        {
            var workflowId = Guid.NewGuid();
            var design = new HouseDesign { Id = Guid.NewGuid(), WorkflowStateId = workflowId, Version = 1, DesignSource = "test", FloorCount = 1, TotalBuiltUpAreaSqft = 100, LayoutJson = "{}", IsCurrent = true };
            _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = _clientId, Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
            var contractorId = Guid.NewGuid();
            _dbContext.Projects.Add(new Project { Id = Guid.NewGuid(), WorkflowStateId = workflowId, HouseDesignId = design.Id, ContractorId = contractorId, Status = "Cancelled" });
            await _dbContext.SaveChangesAsync();

            var result = Assert.IsType<OkObjectResult>(await _controller.GetConstruction());
            var data = result.Value as dynamic;
            // Depending on anonymous type, we use reflection or dynamic. We know it doesn't throw.
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var activeProjects = doc.RootElement.GetProperty("activeProjects");
            Assert.Equal(0, activeProjects.GetArrayLength());
        }
    }
}
