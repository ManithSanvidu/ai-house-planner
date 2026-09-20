using Microsoft.AspNetCore.Mvc;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace HousePlanner.API.Controllers
{
    [ApiController]
    [Route("api/v1/ai-generation")]
    public class AiGenerationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _agenticServiceClient;
        private readonly IDesignOptionsService _designOptionsService;
        private readonly ICurrentUserContextService _currentUser;

        public AiGenerationController(ApplicationDbContext context, IHttpClientFactory httpClientFactory,
            IDesignOptionsService designOptionsService, ICurrentUserContextService currentUser)
        {
            _context = context;
            _agenticServiceClient = httpClientFactory.CreateClient("AgenticService");
            _designOptionsService = designOptionsService;
            _currentUser = currentUser;
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] AiGenerationRequest request, CancellationToken cancellationToken)
        {
            var authenticatedUser = await _currentUser.GetAsync(HttpContext);
            if (authenticatedUser?.Id is null) return Unauthorized(new { message = "User not identified." });
            if (!string.Equals(authenticatedUser.Role, "Customer", StringComparison.OrdinalIgnoreCase)) return Forbid();
            if (!string.IsNullOrEmpty(request.ManualTerrainType))
            {
                if (request.ManualTerrainType.StartsWith("flat", StringComparison.OrdinalIgnoreCase))
                    request.ManualTerrainType = "flat";
                else if (request.ManualTerrainType.StartsWith("hillside", StringComparison.OrdinalIgnoreCase))
                    request.ManualTerrainType = "hillside";
                else if (request.ManualTerrainType.StartsWith("coastal", StringComparison.OrdinalIgnoreCase))
                    request.ManualTerrainType = "coastal";
                else
                    request.ManualTerrainType = "unknown";
            }

            var validation = await _designOptionsService.ValidateFinalSelectionAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return BadRequest(new { 
                    code = validation.ErrorCode, 
                    message = validation.Message,
                    conflicts = validation.Conflicts,
                    suggestions = validation.Suggestions 
                });
            }

            WorkflowState workflowState;
            object payload;
            try 
            {
                // Ownership always comes from the validated Firebase identity. ClientId is retained
                // in the DTO for wire compatibility but is never trusted for authorization.
                var client = await _context.Users.FindAsync(authenticatedUser.Id.Value);
                if (client is null)
                    return Conflict(new { Message = "No client account exists for this submission." });
                PreDesignedHousePlan? basePlan = null;
                if (request.BasePreDesignedPlanId.HasValue)
                {
                    basePlan = await _context.PreDesignedHousePlans.FirstOrDefaultAsync(p => p.Id == request.BasePreDesignedPlanId && p.IsActive);
                    if (basePlan is null) return BadRequest(new { Message = "The selected pre-designed plan is unavailable." });
                }

                var submission = new LandSubmission
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    BudgetLkr = request.BudgetLkr ?? 0m,
                    LandSizePerches = request.LandSizePerches,
                    ManualTerrainType = request.ManualTerrainType,
                    PreferredBedrooms = request.Preferences?.Bedrooms ?? 3,
                    PreferredFloors = request.Preferences?.Floors ?? 1,
                    StylePreference = request.Preferences?.ArchitecturalStyle ?? "Modern Minimalist",
                    BasePreDesignedPlanId = basePlan?.Id,
                    PlanSelectionMode = request.PlanSelectionMode,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                _context.LandSubmissions.Add(submission);
                
                workflowState = new WorkflowState
                {
                    Id = Guid.NewGuid(),
                    LandSubmissionId = submission.Id,
                    Status = "running",
                    ApprovalStatus = "not_requested",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                
                _context.WorkflowStates.Add(workflowState);
                await _context.SaveChangesAsync();

                if (basePlan is not null && string.Equals(request.PlanSelectionMode, "use", StringComparison.OrdinalIgnoreCase))
                {
                    using var document = JsonDocument.Parse(basePlan.LayoutJson);
                    var root = document.RootElement;
                    var design = new HouseDesign
                    {
                        WorkflowStateId = workflowState.Id, Version = 1, FloorCount = basePlan.FloorCount,
                        TotalBuiltUpAreaSqft = basePlan.TotalBuiltUpAreaSqft,
                        FoundationType = root.TryGetProperty("foundation_type", out var foundation) ? foundation.GetString() ?? "conceptual" : "conceptual",
                        TemplateId = root.TryGetProperty("template_id", out var template) ? template.GetString() : null,
                        TerrainType = basePlan.SuitableTerrain, LayoutJson = basePlan.LayoutJson, IsCurrent = true,
                        DesignSource = "pre_designed", BasePreDesignedPlanId = basePlan.Id, CreatedAt = DateTimeOffset.UtcNow
                    };
                    foreach (var room in root.GetProperty("rooms").EnumerateArray())
                    {
                        var width = room.GetProperty("width").GetDecimal(); var length = room.GetProperty("length").GetDecimal();
                        design.Rooms.Add(new Room { RoomType=room.GetProperty("room_type").GetString()??"unknown", Name=room.TryGetProperty("name",out var n)?n.GetString():null, FloorNumber=room.GetProperty("floor").GetInt32(), X=room.GetProperty("x").GetDecimal(), Y=room.GetProperty("y").GetDecimal(), Width=width, Length=length, AreaSqft=Math.Round(width*length,2), WallHeight=room.TryGetProperty("wall_height",out var wh)?wh.GetDecimal():9 });
                    }
                    workflowState.Status = "design_generated"; _context.HouseDesigns.Add(design); await _context.SaveChangesAsync();
                    return Ok(new { Message = "Pre-designed plan selected successfully", WorkflowId = workflowState.Id });
                }

                payload = new {
                    workflow_id = workflowState.Id,
                    submission_id = submission.Id,
                    budget_lkr = request.BudgetLkr,
                    land_size_perches = request.LandSizePerches,
                    manual_terrain_type = request.ManualTerrainType,
                    preferences = request.Preferences,
                    plot_constraints = request.PlotConstraints,
                    design_seed = request.DesignSeed
                    ,base_pre_designed_plan_id = request.BasePreDesignedPlanId
                    ,plan_selection_mode = request.PlanSelectionMode
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Database error while saving the submission.", Details = ex.InnerException?.Message ?? ex.Message });
            }

            var options = new JsonSerializerOptions { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            var content = new StringContent(JsonSerializer.Serialize(payload, options), Encoding.UTF8, "application/json");
            try
            {
                var response = await _agenticServiceClient.PostAsync("/workflows/start", content);
                if (!response.IsSuccessStatusCode)
                {
                    // If the Python API returns a 4xx or 5xx, we handle it gracefully instead of a raw 500
                    return BadRequest(new { Message = $"Agentic service returned an error: {response.StatusCode}" });
                }
            }
            catch (HttpRequestException ex)
            {
                // This means the Python backend is NOT running or is unreachable
                return BadRequest(new { Message = "Cannot connect to the AI Agentic Service. Please make sure it is running on port 8001.", Details = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An unexpected error occurred while calling the AI service.", Details = ex.Message });
            }

            return Ok(new { Message = "Workflow started successfully", WorkflowId = workflowState.Id });
        }
    }

    public class AiGenerationRequest
    {
        public Guid? ClientId { get; set; }
        public decimal? BudgetLkr { get; set; }
        public decimal LandSizePerches { get; set; }
        public string? ManualTerrainType { get; set; }
        public PreferencesDto? Preferences { get; set; }
        public PlotConstraintsDto? PlotConstraints { get; set; }
        public long? DesignSeed { get; set; }
        public Guid? BasePreDesignedPlanId { get; set; }
        public string? PlanSelectionMode { get; set; }
    }

    public class PreferencesDto
    {
        public int Bedrooms { get; set; }
        public int Floors { get; set; }
        public string? ArchitecturalStyle { get; set; }
        public string? LandUnit { get; set; }
        public int? Bathrooms { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("open_plan")]
        public bool? OpenPlan { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("master_ensuite")]
        public bool? MasterEnsuite { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("separate_dining")]
        public bool? SeparateDining { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("home_office")]
        public bool? HomeOffice { get; set; }
        public bool? Balcony { get; set; }
        public bool? Veranda { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("utility_room")]
        public bool? UtilityRoom { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("parking_required")]
        public bool? ParkingRequired { get; set; }
        public bool? Accessibility { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("space_priority")]
        public string? SpacePriority { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("circulation_preference")]
        public string? CirculationPreference { get; set; }
    }

    public class PlotConstraintsDto
    {
        public string? road_side { get; set; }
        public decimal? plot_width_ft { get; set; }
        public decimal? plot_length_ft { get; set; }
        public string? north_direction { get; set; }
        public string? entrance_side { get; set; }
        public SetbacksDto? setbacks { get; set; }
    }

    public class SetbacksDto
    {
        public decimal? front { get; set; }
        public decimal? rear { get; set; }
        public decimal? left { get; set; }
        public decimal? right { get; set; }
    }
}
