using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HousePlanner.API.Tests.Controllers;

public class InternalWorkflowControllerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<InternalWorkflowController>> _loggerMock;
    private readonly InternalWorkflowController _controller;

    public InternalWorkflowControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<InternalWorkflowController>>();
        _controller = new InternalWorkflowController(_dbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task UpdateTerrain_UpdatesWorkflow_Successfully()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowState
        {
            Id = workflowId,
            LandSubmissionId = Guid.NewGuid(),
            Status = "pending"
        };
        _dbContext.WorkflowStates.Add(workflow);
        await _dbContext.SaveChangesAsync();

        var json = @"{
            ""terrain_type"": ""hillside"",
            ""slope_estimate"": ""moderate"",
            ""notable_features"": [""trees""]
        }";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = await _controller.UpdateTerrain(workflowId, element);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        
        var dbWorkflow = await _dbContext.WorkflowStates.FindAsync(workflowId);
        Assert.Equal("hillside", dbWorkflow!.TerrainType);
        Assert.Equal("moderate", dbWorkflow.SlopeEstimate);
        Assert.Contains("trees", dbWorkflow.NotableFeatures);
    }

    [Fact]
    public async Task SubmitDesignRevision_CreatesNewVersion_AndMarksOldAsNotCurrent()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var oldDesign = new HouseDesign
        {
            Id = Guid.NewGuid(),
            WorkflowStateId = workflowId,
            Version = 1,
            IsCurrent = true,
            FloorCount = 1,
            TotalBuiltUpAreaSqft = 1000m,
            FoundationType = "slab",
            LayoutJson = "{}"
        };

        var workflow = new WorkflowState
        {
            Id = workflowId,
            LandSubmissionId = Guid.NewGuid(),
            Status = "pending",
            HouseDesigns = new List<HouseDesign> { oldDesign }
        };
        _dbContext.WorkflowStates.Add(workflow);
        await _dbContext.SaveChangesAsync();

        var json = @"{
            ""floor_count"": 2,
            ""total_built_up_area_sqft"": 1500.5,
            ""foundation_type"": ""stepped"",
            ""template_id"": ""TEST_TEMPLATE"",
            ""terrain_type"": ""hillside"",
            ""geometry_fingerprint"": ""abc123"",
            ""candidate_summary"": { ""selected_plan_code"": ""HP-TEST"", ""generation_mode"": ""deterministic_template_selection"" },
            ""rooms"": [
                {
                    ""room_type"": ""kitchen"",
                    ""floor"": 1,
                    ""x"": 0,
                    ""y"": 0,
                    ""width"": 12,
                    ""length"": 10,
                    ""wall_height"": 9
                }
            ]
        }";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = await _controller.SaveDesign(workflowId, element);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var dbWorkflow = await _dbContext.WorkflowStates
            .Include(w => w.HouseDesigns)
            .ThenInclude(d => d.Rooms)
            .FirstOrDefaultAsync(w => w.Id == workflowId);

        Assert.Equal("design_generated", dbWorkflow!.Status);
        Assert.Equal("hillside", dbWorkflow.TerrainType);

        var oldDbDesign = dbWorkflow.HouseDesigns.FirstOrDefault(d => d.Id == oldDesign.Id);
        Assert.False(oldDbDesign!.IsCurrent);

        var newDbDesign = dbWorkflow.HouseDesigns.FirstOrDefault(d => d.Id != oldDesign.Id);
        Assert.NotNull(newDbDesign);
        Assert.True(newDbDesign.IsCurrent);
        Assert.Equal(2, newDbDesign.Version);
        Assert.Equal(2, newDbDesign.FloorCount);
        Assert.Equal(1500.5m, newDbDesign.TotalBuiltUpAreaSqft);
        Assert.Equal("stepped", newDbDesign.FoundationType);
        using (var persisted = JsonDocument.Parse(newDbDesign.LayoutJson))
        {
            Assert.Equal("abc123", persisted.RootElement.GetProperty("geometry_fingerprint").GetString());
            Assert.Equal("HP-TEST", persisted.RootElement.GetProperty("candidate_summary")
                .GetProperty("selected_plan_code").GetString());
        }
        
        Assert.Single(newDbDesign.Rooms);
        var room = newDbDesign.Rooms.First();
        Assert.Equal("kitchen", room.RoomType);
        Assert.Equal(1, room.FloorNumber);
        Assert.Equal(12m, room.Width);
        Assert.Equal(10m, room.Length);
        Assert.Equal(120m, room.AreaSqft);
    }

    [Fact]
    public async Task UnknownCallbacksReturnNotFoundWithoutCreatingRecords()
    {
        var workflowId = Guid.NewGuid();
        using var failureJson = JsonDocument.Parse("{\"status\":\"failed\"}");
        using var terrainJson = JsonDocument.Parse("{\"terrain_type\":\"flat\"}");
        using var designJson = JsonDocument.Parse(
            "{\"floor_count\":1,\"total_built_up_area_sqft\":100," +
            "\"foundation_type\":\"slab\",\"rooms\":[]}");

        Assert.IsType<NotFoundObjectResult>(
            await _controller.UpdateGenerationStatus(workflowId, failureJson.RootElement));
        Assert.IsType<NotFoundObjectResult>(
            await _controller.UpdateTerrain(workflowId, terrainJson.RootElement));
        Assert.IsType<NotFoundObjectResult>(
            await _controller.SaveDesign(workflowId, designJson.RootElement));
        Assert.IsType<NotFoundObjectResult>(
            await _controller.SaveCostEstimate(workflowId, new SaveCostEstimateRequestDto
            {
                MaterialCostLkr = 1000m,
                LabourCostLkr = 300m,
                TotalCostLkr = 1300m,
                BudgetDeltaPercent = 13m
            }));
        Assert.Empty(_dbContext.WorkflowStates);
        Assert.Empty(_dbContext.HouseDesigns);
        Assert.Empty(_dbContext.CostEstimates);
    }

    [Fact]
    public async Task SaveCostEstimate_SuccessfullyPersists_AndLinksToCurrentDesign()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var designId = Guid.NewGuid();
        var design = new HouseDesign
        {
            Id = designId,
            WorkflowStateId = workflowId,
            Version = 1,
            IsCurrent = true,
            FloorCount = 1,
            TotalBuiltUpAreaSqft = 1000m,
            FoundationType = "slab",
            LayoutJson = "{}"
        };

        var workflow = new WorkflowState
        {
            Id = workflowId,
            LandSubmissionId = Guid.NewGuid(),
            Status = "design_generated",
            HouseDesigns = new List<HouseDesign> { design }
        };

        _dbContext.WorkflowStates.Add(workflow);
        await _dbContext.SaveChangesAsync();

        var request = new SaveCostEstimateRequestDto
        {
            MaterialCostLkr = 500000m,
            LabourCostLkr = 150000m,
            TotalCostLkr = 650000m,
            BudgetDeltaPercent = 13.0m
        };

        // Act
        var result = await _controller.SaveCostEstimate(workflowId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CostEstimateResponseDto>(okResult.Value);

        Assert.NotEqual(Guid.Empty, response.CostEstimateId);
        Assert.Equal(designId, response.HouseDesignId);
        Assert.Equal(500000m, response.MaterialCostLkr);
        Assert.Equal(150000m, response.LabourCostLkr);
        Assert.Equal(650000m, response.TotalCostLkr);
        Assert.Equal(13.0m, response.BudgetDeltaPercent);

        var dbEstimate = await _dbContext.CostEstimates.FirstOrDefaultAsync(c => c.Id == response.CostEstimateId);
        Assert.NotNull(dbEstimate);
        Assert.Equal(designId, dbEstimate.HouseDesignId);
        Assert.Equal(500000m, dbEstimate.MaterialCostLkr);
        Assert.Equal(150000m, dbEstimate.LabourCostLkr);
        Assert.Equal(650000m, dbEstimate.TotalCostLkr);
        Assert.Equal(13.0m, dbEstimate.BudgetDeltaPercent);
    }

    [Fact]
    public async Task SaveCostEstimate_WorkflowNotFound_ReturnsNotFound()
    {
        var nonExistentWorkflowId = Guid.NewGuid();
        var request = new SaveCostEstimateRequestDto
        {
            MaterialCostLkr = 1000m,
            LabourCostLkr = 300m,
            TotalCostLkr = 1300m,
            BudgetDeltaPercent = 13m
        };

        var result = await _controller.SaveCostEstimate(nonExistentWorkflowId, request);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(_dbContext.CostEstimates);
    }

    [Fact]
    public async Task SaveCostEstimate_NoCurrentDesign_ReturnsConflict()
    {
        // Arrange: Workflow exists, but has no HouseDesigns or none with IsCurrent == true
        var workflowId = Guid.NewGuid();
        var oldDesign = new HouseDesign
        {
            Id = Guid.NewGuid(),
            WorkflowStateId = workflowId,
            Version = 1,
            IsCurrent = false,
            FloorCount = 1,
            TotalBuiltUpAreaSqft = 1000m,
            FoundationType = "slab",
            LayoutJson = "{}"
        };

        var workflow = new WorkflowState
        {
            Id = workflowId,
            LandSubmissionId = Guid.NewGuid(),
            Status = "pending",
            HouseDesigns = new List<HouseDesign> { oldDesign }
        };

        _dbContext.WorkflowStates.Add(workflow);
        await _dbContext.SaveChangesAsync();

        var request = new SaveCostEstimateRequestDto
        {
            MaterialCostLkr = 1000m,
            LabourCostLkr = 300m,
            TotalCostLkr = 1300m,
            BudgetDeltaPercent = 13m
        };

        // Act
        var result = await _controller.SaveCostEstimate(workflowId, request);

        // Assert
        Assert.IsType<ConflictObjectResult>(result);
        Assert.Empty(_dbContext.CostEstimates);
    }

    [Theory]
    [InlineData(-1, 100, 99, 10)]
    [InlineData(100, -1, 99, 10)]
    [InlineData(100, 100, -1, 10)]
    [InlineData(100, 100, 200, -5)]
    public async Task SaveCostEstimate_NegativeValues_ReturnsBadRequest(
        decimal material, decimal labour, decimal total, decimal budgetDelta)
    {
        var workflowId = Guid.NewGuid();
        var request = new SaveCostEstimateRequestDto
        {
            MaterialCostLkr = material,
            LabourCostLkr = labour,
            TotalCostLkr = total,
            BudgetDeltaPercent = budgetDelta
        };

        var result = await _controller.SaveCostEstimate(workflowId, request);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_dbContext.CostEstimates);
    }

    [Fact]
    public async Task SaveCostEstimate_InconsistentTotal_ReturnsBadRequest()
    {
        var workflowId = Guid.NewGuid();
        var request = new SaveCostEstimateRequestDto
        {
            MaterialCostLkr = 1000m,
            LabourCostLkr = 300m,
            TotalCostLkr = 2000m, // Inconsistent: 1000 + 300 != 2000
            BudgetDeltaPercent = 20m
        };

        var result = await _controller.SaveCostEstimate(workflowId, request);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_dbContext.CostEstimates);
    }

    [Fact]
    public async Task SaveCostEstimate_RetryDoesNotCreateDuplicateEstimates_UpdatesExisting()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var designId = Guid.NewGuid();
        var design = new HouseDesign
        {
            Id = designId,
            WorkflowStateId = workflowId,
            Version = 1,
            IsCurrent = true,
            FloorCount = 1,
            TotalBuiltUpAreaSqft = 1000m,
            FoundationType = "slab",
            LayoutJson = "{}"
        };

        var workflow = new WorkflowState
        {
            Id = workflowId,
            LandSubmissionId = Guid.NewGuid(),
            Status = "design_generated",
            HouseDesigns = new List<HouseDesign> { design }
        };

        _dbContext.WorkflowStates.Add(workflow);
        await _dbContext.SaveChangesAsync();

        var initialRequest = new SaveCostEstimateRequestDto
        {
            MaterialCostLkr = 400000m,
            LabourCostLkr = 120000m,
            TotalCostLkr = 520000m,
            BudgetDeltaPercent = 10.4m
        };

        var retryRequest = new SaveCostEstimateRequestDto
        {
            MaterialCostLkr = 450000m,
            LabourCostLkr = 135000m,
            TotalCostLkr = 585000m,
            BudgetDeltaPercent = 11.7m
        };

        // Act: Submit twice for the same workflow/design
        var firstResult = await _controller.SaveCostEstimate(workflowId, initialRequest);
        var secondResult = await _controller.SaveCostEstimate(workflowId, retryRequest);

        // Assert
        var firstOk = Assert.IsType<OkObjectResult>(firstResult);
        var secondOk = Assert.IsType<OkObjectResult>(secondResult);

        var firstDto = Assert.IsType<CostEstimateResponseDto>(firstOk.Value);
        var secondDto = Assert.IsType<CostEstimateResponseDto>(secondOk.Value);

        // Exactly 1 estimate in DB for this design (not duplicated)
        var estimates = await _dbContext.CostEstimates.Where(c => c.HouseDesignId == designId).ToListAsync();
        Assert.Single(estimates);

        var currentEstimate = estimates.First();
        Assert.Equal(firstDto.CostEstimateId, currentEstimate.Id);
        Assert.Equal(secondDto.CostEstimateId, currentEstimate.Id);
        Assert.Equal(450000m, currentEstimate.MaterialCostLkr);
        Assert.Equal(135000m, currentEstimate.LabourCostLkr);
        Assert.Equal(585000m, currentEstimate.TotalCostLkr);
        Assert.Equal(11.7m, currentEstimate.BudgetDeltaPercent);
    }

    [Fact]
    public void CostEstimate_HouseDesignIndex_IsUnique()
    {
        var entityType = _dbContext.Model.FindEntityType(typeof(CostEstimate));
        var index = entityType!.GetIndexes()
            .Single(i => i.Properties.Single().Name == nameof(CostEstimate.HouseDesignId));

        Assert.True(index.IsUnique);
    }
}
