using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HousePlanner.API.Controllers;

[ApiController]
[Route("api/v1/assistant")]
public class AssistantController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AssistantController> _logger;

    public AssistantController(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<AssistantController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    [HttpPost("interpret")]
    public async Task<IActionResult> InterpretMessage([FromBody] AssistantInterpretRequest request)
    {
        try
        {
            var internalApiKey = _config["AgenticService:InternalApiKey"];
            var agenticServiceUrl = _config["AgenticService:BaseUrl"];

            if (string.IsNullOrEmpty(agenticServiceUrl) || string.IsNullOrEmpty(internalApiKey))
            {
                _logger.LogError("Agentic service configuration is missing.");
                return StatusCode(500, "Service unavailable");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Internal-API-Key", internalApiKey);

            var payload = new { message = request.Message, history = request.History };
            var response = await client.PostAsJsonAsync($"{agenticServiceUrl.TrimEnd('/')}/assistant/interpret", payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Agentic service returned an error: {response.StatusCode} {error}");
                return StatusCode((int)response.StatusCode, "Assistant service failed");
            }

            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling assistant service");
            return StatusCode(500, "Internal server error");
        }
    }
}

public class AssistantMessageEntry
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class AssistantInterpretRequest
{
    public string Message { get; set; } = string.Empty;
    public List<AssistantMessageEntry>? History { get; set; }
}
