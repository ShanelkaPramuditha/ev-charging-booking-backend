/*
 * File Name: HealthController.cs
 * Description: Simple health check endpoints for service monitoring.
 */

using Microsoft.AspNetCore.Mvc;

namespace EadChargingBookingBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}