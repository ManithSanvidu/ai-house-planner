using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Moq;
using HousePlanner.API.Services;
using System.Linq;
using System.Collections.Generic;

namespace HousePlanner.API.Tests.Controllers
{
    public partial class ConstructorWorkflowControllerTests
    {
        [Fact]
        public async Task CancelledProject_NotReturnedInConstructorActiveProjects()
        {
            var workflowId = Guid.NewGuid();
            var design = new HouseDesign { Id = Guid.NewGuid(), WorkflowStateId = workflowId, Version = 1, DesignSource = "test", FloorCount = 1, TotalBuiltUpAreaSqft = 100, LayoutJson = "{}", IsCurrent = true };
            _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
            var contractorId = _constructorId;
            _dbContext.Projects.Add(new Project { Id = Guid.NewGuid(), WorkflowStateId = workflowId, HouseDesignId = design.Id, ContractorId = contractorId, Status = "Cancelled" });
            await _dbContext.SaveChangesAsync();

            var result = Assert.IsType<OkObjectResult>(await _controller.GetProjects());
            var projects = Assert.IsAssignableFrom<IEnumerable<ConstructorProjectDto>>(result.Value);
            Assert.Empty(projects);
        }
    }
}
