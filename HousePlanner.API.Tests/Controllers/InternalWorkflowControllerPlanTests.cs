using System;
using System.Text.Json;
using System.Threading.Tasks;
using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace HousePlanner.API.Tests.Controllers
{
    public class InternalWorkflowControllerPlanTests
    {
        private async Task<ApplicationDbContext> GetDbContextAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return context;
        }

        [Fact]
        public async Task UpdatePlan_ExistingWorkflow_SavesPlanJsonAndReturnsOk()
        {
            var context = await GetDbContextAsync();
            var submissionId = Guid.NewGuid();
            context.LandSubmissions.Add(new LandSubmission { Id = submissionId, PreferredBedrooms = 3 });
            var workflowId = Guid.NewGuid();
            var workflow = new WorkflowState { Id = workflowId, LandSubmissionId = submissionId };
            context.WorkflowStates.Add(workflow);
            await context.SaveChangesAsync();

            var controller = new InternalWorkflowController(context, new NullLogger<InternalWorkflowController>());
            var request = new WorkflowPlanStateRequest
            {
                Plan = JsonSerializer.Deserialize<JsonElement>("{\"test\":\"value\"}"),
                CurrentStepId = "S1",
                CompletedStepIds = new System.Collections.Generic.List<string> { "S0" }
            };

            var result = await controller.UpdatePlan(workflowId, request);

            Assert.IsType<OkObjectResult>(result);
            var updatedWorkflow = await context.WorkflowStates.FindAsync(workflowId);
            Assert.NotNull(updatedWorkflow.PlanJson);
            Assert.Contains("S1", updatedWorkflow.PlanJson);
        }

        [Fact]
        public async Task UpdatePlan_MissingWorkflow_ReturnsNotFound()
        {
            var context = await GetDbContextAsync();
            var controller = new InternalWorkflowController(context, new NullLogger<InternalWorkflowController>());
            var request = new WorkflowPlanStateRequest
            {
                Plan = JsonSerializer.Deserialize<JsonElement>("{}")
            };

            var result = await controller.UpdatePlan(Guid.NewGuid(), request);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
