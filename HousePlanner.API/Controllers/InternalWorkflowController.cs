using System.Text.Json;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Controllers;

[ApiController]
[Route("api/v1/internal/workflows")]
// Requires InternalServiceAuthMiddleware — shared secret header, not JWT
public class InternalWorkflowController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InternalWorkflowController> _logger;

    public InternalWorkflowController(ApplicationDbContext context, ILogger<InternalWorkflowController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private async Task<WorkflowState?> FindWorkflowState(Guid workflowId)
    {
        var workflow = await _context.WorkflowStates
            .Include(w => w.HouseDesigns)
                .ThenInclude(d => d.CostEstimates)
            .FirstOrDefaultAsync(w => w.Id == workflowId);

        return workflow;
    }

    /// <summary>
    /// Internal endpoint for the Design Agent to submit a new generated layout.
    /// Used both for initial generation and revisions after validation failure.
    /// Manages design versioning — marks old versions as not current.
    /// </summary>
    [HttpPost("{id:guid}/design")]
    public async Task<IActionResult> SubmitDesignRevision(Guid id, [FromBody] JsonElement layoutData)
    {
        try
        {
            var workflow = await FindWorkflowState(id);
            if (workflow is null)
                return NotFound(new { message = $"Unknown workflow {id}; callback was not persisted." });

            // Extract required fields from the JSON contract
            int floorCount = layoutData.GetProperty("floor_count").GetInt32();
            decimal totalArea = layoutData.TryGetProperty("total_built_up_area_sqft", out var areaProp)
                ? areaProp.GetDecimal()
                : 0m;

            string foundationType = layoutData.TryGetProperty("foundation_type", out var foundProp)
                ? foundProp.GetString() ?? "unknown"
                : "unknown";

            string? templateId = layoutData.TryGetProperty("template_id", out var tmplProp)
                ? tmplProp.GetString()
                : null;

            string? terrainType = layoutData.TryGetProperty("terrain_type", out var terrProp)
                ? terrProp.GetString()
                : null;

            // Mark all existing designs for this workflow as not current
            foreach (var existing in workflow.HouseDesigns.Where(d => d.IsCurrent))
            {
                existing.IsCurrent = false;
            }

            var newDesign = new HouseDesign
            {
                WorkflowStateId = id,
                Version = (workflow.HouseDesigns.Count == 0 ? 0 : workflow.HouseDesigns.Max(d => d.Version)) + 1,
                FloorCount = floorCount,
                TotalBuiltUpAreaSqft = totalArea,
                FoundationType = foundationType,
                TemplateId = templateId,
                TerrainType = terrainType,
                IsCurrent = true, // New design is always current
                LayoutJson = layoutData.GetRawText(),
                CreatedAt = DateTimeOffset.UtcNow,
                Rooms = new List<Room>()
            };

            // Parse rooms into the relational DB format for querying/validation
            if (layoutData.TryGetProperty("rooms", out var roomsElement) && roomsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var roomEl in roomsElement.EnumerateArray())
                {
                    var roomType = roomEl.GetProperty("room_type").GetString() ?? "unknown";
                    var width = roomEl.GetProperty("width").GetDecimal();
                    var length = roomEl.GetProperty("length").GetDecimal();

                    var room = new Room
                    {
                        RoomType = roomType,
                        Name = roomEl.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
                        FloorNumber = roomEl.GetProperty("floor").GetInt32(),
                        X = roomEl.GetProperty("x").GetDecimal(),
                        Y = roomEl.GetProperty("y").GetDecimal(),
                        Width = width,
                        Length = length,
                        AreaSqft = Math.Round(width * length, 2),
                        WallHeight = roomEl.TryGetProperty("wall_height", out var wh) ? wh.GetDecimal() : 9.0m
                    };
                    newDesign.Rooms.Add(room);
                }
            }

            _context.HouseDesigns.Add(newDesign);

            // Update workflow status and terrain
            workflow.Status = "design_generated";
            workflow.FailureReason = null;
            if (terrainType != null)
                workflow.TerrainType ??= terrainType;
            workflow.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully saved design revision v{Version} for workflow {WorkflowId}", newDesign.Version, id);
            return Ok(new { message = "Design saved successfully.", designId = newDesign.Id, version = newDesign.Version });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = "Missing required fields in layout JSON.", error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving design for workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred saving the design." });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateGenerationStatus(Guid id, [FromBody] JsonElement data)
    {
        if (!data.TryGetProperty("status", out var status) || status.GetString() != "failed")
            return BadRequest(new { message = "This endpoint accepts only generation failure." });
        var workflow = await FindWorkflowState(id);
        if (workflow is null) return NotFound(new { message = $"Unknown workflow {id}." });
        workflow.Status = "failed";
        workflow.ApprovalStatus = "not_requested";
        var reasonText = data.TryGetProperty("reason", out var reason)
            ? reason.GetString() ?? "Design generation failed."
            : "Design generation failed without a detailed reason.";
        workflow.FailureReason = reasonText[..Math.Min(reasonText.Length, 1000)];
        workflow.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { status = workflow.Status });
    }

    /// <summary>
    /// Internal endpoint for the Land Analysis Agent to update terrain results.
    /// </summary>
    [HttpPatch("{id:guid}/terrain")]
    public async Task<IActionResult> UpdateTerrain(Guid id, [FromBody] JsonElement terrainData)
    {
        try
        {
            var workflow = await FindWorkflowState(id);
            if (workflow is null) return NotFound(new { message = $"Unknown workflow {id}." });

            if (terrainData.TryGetProperty("terrain_type", out var terrainProp))
                workflow.TerrainType = terrainProp.GetString();

            if (terrainData.TryGetProperty("slope_estimate", out var slopeProp))
                workflow.SlopeEstimate = slopeProp.GetString();

            if (terrainData.TryGetProperty("notable_features", out var featuresProp))
                workflow.NotableFeatures = featuresProp.GetRawText();

            workflow.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Terrain updated for workflow {WorkflowId}: {TerrainType}", id, workflow.TerrainType);
            return Ok(new { message = "Terrain updated.", terrainType = workflow.TerrainType });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating terrain for workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred updating terrain." });
        }
    }

    /// <summary>
    /// Internal endpoint for the Construction Planning Agent to update construction plan results.
    /// </summary>
    [HttpPatch("{id:guid}/construction-plan")]
    public async Task<IActionResult> UpdateConstructionPlan(Guid id, [FromBody] JsonElement planData)
    {
        try
        {
            var workflow = await FindWorkflowState(id);
            if (workflow is null) return NotFound(new { message = $"Unknown workflow {id}." });

            workflow.ConstructionPlan = planData.GetRawText();
            workflow.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Construction plan updated for workflow {WorkflowId}", id);
            return Ok(new { message = "Construction plan updated." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating construction plan for workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred updating the construction plan." });
        }
    }

    /// <summary>
    /// Internal endpoint for the Cost Estimation Agent to persist a calculated cost estimate.
    /// Links to the current house design version for the specified workflow.
    /// </summary>
    [HttpPost("{id:guid}/cost-estimate")]
    public async Task<IActionResult> SaveCostEstimate(Guid id, [FromBody] SaveCostEstimateRequestDto request)
    {
        try
        {
            if (request is null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (request.MaterialCostLkr < 0)
                return BadRequest(new { message = "Material cost cannot be negative." });

            if (request.LabourCostLkr < 0)
                return BadRequest(new { message = "Labour cost cannot be negative." });

            if (request.TotalCostLkr < 0)
                return BadRequest(new { message = "Total cost cannot be negative." });

            if (request.BudgetDeltaPercent < 0)
                return BadRequest(new { message = "Budget delta percent cannot be negative." });

            const decimal tolerance = 0.05m;
            if (Math.Abs(request.TotalCostLkr - (request.MaterialCostLkr + request.LabourCostLkr)) > tolerance)
            {
                return BadRequest(new { message = $"Total cost ({request.TotalCostLkr}) does not match the sum of material ({request.MaterialCostLkr}) and labour ({request.LabourCostLkr}) within tolerance." });
            }

            var workflow = await FindWorkflowState(id);
            if (workflow is null)
                return NotFound(new { message = $"Unknown workflow {id}; cost estimate was not persisted." });

            var currentDesign = workflow.HouseDesigns.FirstOrDefault(d => d.IsCurrent);
            if (currentDesign is null)
                return Conflict(new { message = "No current house design exists for this workflow." });

            // Retry/idempotency: update existing estimate for this current design if present, otherwise create new
            var existingEstimate = currentDesign.CostEstimates
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            CostEstimate estimate;
            if (existingEstimate != null)
            {
                existingEstimate.MaterialCostLkr = request.MaterialCostLkr;
                existingEstimate.LabourCostLkr = request.LabourCostLkr;
                existingEstimate.TotalCostLkr = request.TotalCostLkr;
                existingEstimate.BudgetDeltaPercent = request.BudgetDeltaPercent;
                estimate = existingEstimate;
            }
            else
            {
                estimate = new CostEstimate
                {
                    HouseDesignId = currentDesign.Id,
                    MaterialCostLkr = request.MaterialCostLkr,
                    LabourCostLkr = request.LabourCostLkr,
                    TotalCostLkr = request.TotalCostLkr,
                    BudgetDeltaPercent = request.BudgetDeltaPercent,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _context.CostEstimates.Add(estimate);
            }

            workflow.UpdatedAt = DateTimeOffset.UtcNow;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException) when (existingEstimate is null)
            {
                // A concurrent retry may have inserted the unique row after our read.
                _context.Entry(estimate).State = EntityState.Detached;
                var concurrentEstimate = await _context.CostEstimates
                    .SingleOrDefaultAsync(c => c.HouseDesignId == currentDesign.Id);
                if (concurrentEstimate is null)
                    throw;

                concurrentEstimate.MaterialCostLkr = request.MaterialCostLkr;
                concurrentEstimate.LabourCostLkr = request.LabourCostLkr;
                concurrentEstimate.TotalCostLkr = request.TotalCostLkr;
                concurrentEstimate.BudgetDeltaPercent = request.BudgetDeltaPercent;
                await _context.SaveChangesAsync();
                estimate = concurrentEstimate;
            }

            _logger.LogInformation(
                "Successfully saved cost estimate {CostEstimateId} for workflow {WorkflowId}, design {DesignId}",
                estimate.Id, id, currentDesign.Id);

            var responseDto = new CostEstimateResponseDto
            {
                CostEstimateId = estimate.Id,
                HouseDesignId = currentDesign.Id,
                MaterialCostLkr = estimate.MaterialCostLkr,
                LabourCostLkr = estimate.LabourCostLkr,
                TotalCostLkr = estimate.TotalCostLkr,
                BudgetDeltaPercent = estimate.BudgetDeltaPercent
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving cost estimate for workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred saving the cost estimate." });
        }
    }
}
