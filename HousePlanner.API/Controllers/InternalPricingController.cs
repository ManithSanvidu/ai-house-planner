using HousePlanner.API.DTOs;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HousePlanner.API.Controllers
{
    [ApiController]
    [Route("api/v1/internal/pricing")]
    public class InternalPricingController : ControllerBase
    {
        private readonly IPricingService _pricingService;

        public InternalPricingController(IPricingService pricingService)
        {
            _pricingService = pricingService;
        }

        /// <summary>
        /// Retrieves all current pricing data for internal services (e.g., Cost Estimation Agent).
        /// Protected by internal API key middleware on /api/v1/internal route prefix.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PricingDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PricingDto>>> GetPricing()
        {
            var pricing = await _pricingService.GetAllPricingAsync();
            return Ok(pricing);
        }
    }
}
