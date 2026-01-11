using Microsoft.AspNetCore.Mvc;

namespace BillingExtractor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly ILogger<SystemController> _logger;

    public SystemController(ILogger<SystemController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get system status and version information
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            Status = "OK",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        });
    }

    /// <summary>
    /// Test LLM connectivity (requires API key)
    /// </summary>
    [HttpGet("test-llm")]
    public IActionResult TestLLM()
    {
        // This would test LLM connectivity
        return Ok(new
        {
            Message = "LLM test endpoint - implementation depends on configured provider",
            ConfiguredProvider = Environment.GetEnvironmentVariable("LLM__PROVIDER") ?? "Not configured"
        });
    }
}