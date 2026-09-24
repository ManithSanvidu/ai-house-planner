using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HousePlanner.API.DTOs;
using HousePlanner.API.Services;
using System.Security.Claims;

namespace HousePlanner.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PricingController : ControllerBase
    {
        private readonly IPricingService _pricingService;

        public PricingController(IPricingService pricingService)
        {
            _pricingService = pricingService;
        }

        /// <summary>
        /// Retrieves all current pricing items.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Constructor")]
        [ProducesResponseType(typeof(IEnumerable<PricingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllPricing()
        {
            var pricingItems = await _pricingService.GetAllPricingAsync();
            return Ok(pricingItems);
        }

        /// <summary>
        /// Creates a new manual pricing item.
        /// </summary>
        /// <param name="createDto">The pricing item data to create.</param>
        [HttpPost]
        [Authorize(Roles = "Constructor")]
        [ProducesResponseType(typeof(PricingDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreatePricing([FromBody] CreatePricingDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (createDto == null)
            {
                return BadRequest("Request body cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(createDto.ItemName))
            {
                return BadRequest("ItemName is required.");
            }

            var category = createDto.Category?.Trim().ToLowerInvariant();
            if (category is not ("material" or "labour"))
            {
                return BadRequest("Category must be 'material' or 'labour'.");
            }

            if (createDto.UnitCostLkr <= 0)
            {
                return BadRequest("UnitCostLkr must be greater than zero.");
            }

            if (createDto.TerrainMultiplier == null ||
                createDto.TerrainMultiplier.Flat <= 0 ||
                createDto.TerrainMultiplier.Hillside <= 0 ||
                createDto.TerrainMultiplier.Coastal <= 0)
            {
                return BadRequest("TerrainMultiplier values must be provided and greater than zero.");
            }

            try
            {
                var created = await _pricingService.CreatePricingAsync(createDto, ActorId());
                return StatusCode(StatusCodes.Status201Created, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Updates a specific pricing item.
        /// </summary>
        /// <param name="id">The ID of the pricing item to update.</param>
        /// <param name="updateDto">The updated pricing data.</param>
        [HttpPut("{id}")]
        [Authorize(Roles = "Constructor")]
        [ProducesResponseType(typeof(PricingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePricing(int id, [FromBody] UpdatePricingDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Additional validation per requirements
            if (updateDto.UnitCostLkr <= 0)
            {
                return BadRequest("UnitCostLkr must be greater than zero.");
            }

            if (updateDto.TerrainMultiplier == null || 
                updateDto.TerrainMultiplier.Flat <= 0 || 
                updateDto.TerrainMultiplier.Hillside <= 0 || 
                updateDto.TerrainMultiplier.Coastal <= 0)
            {
                return BadRequest("TerrainMultiplier values must be provided and greater than zero.");
            }

            try
            {
                var updatedItem = await _pricingService.UpdatePricingAsync(id, updateDto, ActorId());
                if (updatedItem == null)
                    return NotFound($"Pricing item with ID {id} not found.");
                return Ok(updatedItem);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "Constructor")]
        [ProducesResponseType(typeof(PricingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivatePricing(int id, [FromBody] DeactivatePricingDto? request)
        {
            var item = await _pricingService.DeactivatePricingAsync(id, request?.Reason, ActorId());
            return item is null ? NotFound($"Pricing item with ID {id} not found.") : Ok(item);
        }

        [HttpGet("{id}/history")]
        [Authorize(Roles = "Constructor")]
        [ProducesResponseType(typeof(IReadOnlyList<PricingHistoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPricingHistory(int id) =>
            Ok(await _pricingService.GetPricingHistoryAsync(id));

        private string? ActorId() =>
            User?.FindFirstValue("sub") ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
