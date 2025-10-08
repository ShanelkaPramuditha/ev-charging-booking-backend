using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EadChargingBookingBackend.Services;
using EadChargingBookingBackend.DTOs;
using System.Security.Claims;

namespace EadChargingBookingBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all endpoints
public class StationsController : ControllerBase
{
    private readonly IChargingStationService _stationService;

    public StationsController(IChargingStationService stationService)
    {
        _stationService = stationService;
    }

    // Helper to get current user ID from claims
    private string GetCurrentUserId() => User.FindFirstValue("userId") ?? string.Empty;

    // Helper to check if current user has specific role
    private bool HasRole(string role) => User.IsInRole(role);

    /// <summary>
    /// Get all stations (all authenticated users)
    /// </summary>
    /// <returns>List of all stations</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllStations()
    {
        var stations = await _stationService.GetAllStationsAsync();
        return Ok(stations);
    }

    /// <summary>
    /// Get all active stations (all authenticated users)
    /// </summary>
    /// <returns>List of active stations</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveStations()
    {
        var stations = await _stationService.GetActiveStationsAsync();
        return Ok(stations);
    }

    /// <summary>
    /// Get station by ID (all authenticated users)
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <returns>Station details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetStation(string id)
    {
        var station = await _stationService.GetStationByIdAsync(id);
        if (station == null)
            return NotFound(new { Message = "Station not found" });

        return Ok(station);
    }

    /// <summary>
    /// Get stations by operator ID (Backoffice or matching Operator only)
    /// </summary>
    /// <param name="operatorId">Operator ID</param>
    /// <returns>List of stations operated by the specified operator</returns>
    [HttpGet("operator/{operatorId}")]
    public async Task<IActionResult> GetStationsByOperator(string operatorId)
    {
        // Only backoffice users or the operator themselves can access this
        if (!HasRole("backoffice") && GetCurrentUserId() != operatorId)
            return Forbid();

        var stations = await _stationService.GetStationsByOperatorIdAsync(operatorId);
        return Ok(stations);
    }

    /// <summary>
    /// Get nearby stations (all authenticated users)
    /// </summary>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="maxDistance">Maximum distance in kilometers (default: 10)</param>
    /// <param name="limit">Maximum number of results (default: 10)</param>
    /// <returns>List of nearby stations with distance</returns>
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyStations(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double maxDistance = 10,
        [FromQuery] int limit = 10)
    {
        var stations = await _stationService.GetNearbyStationsAsync(latitude, longitude, maxDistance, limit);
        return Ok(stations);
    }

    /// <summary>
    /// Create a new station (Backoffice only)
    /// </summary>
    /// <param name="request">Station creation request</param>
    /// <returns>Created station details</returns>
    [HttpPost]
    [Authorize(Policy = "BackofficeOnly")]
    public async Task<IActionResult> CreateStation([FromBody] CreateStationRequest request)
    {
        var station = await _stationService.CreateStationAsync(request);
        if (station == null)
            return BadRequest(new { Message = "Failed to create station. Name may already be in use." });

        return CreatedAtAction(nameof(GetStation), new { id = station.Id }, station);
    }

    /// <summary>
    /// Update station details (Backoffice only)
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="request">Station update request</param>
    /// <returns>Updated station details</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "BackofficeOnly")]
    public async Task<IActionResult> UpdateStation(string id, [FromBody] UpdateStationRequest request)
    {
        var station = await _stationService.UpdateStationAsync(id, request);
        if (station == null)
            return NotFound(new { Message = "Station not found or name already exists" });

        return Ok(station);
    }

    /// <summary>
    /// Update available slots (Operator or Backoffice)
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="request">Slots update request</param>
    /// <returns>Updated station details</returns>
    [HttpPatch("{id}/slots")]
    [Authorize(Policy = "BackofficeOrOperator")]
    public async Task<IActionResult> UpdateAvailableSlots(string id, [FromBody] UpdateStationSlotsRequest request)
    {
        // If operator, check if they operate this station
        if (HasRole("operator"))
        {
            var station = await _stationService.GetStationByIdAsync(id);
            if (station == null || station.OperatorId != GetCurrentUserId())
                return Forbid();
        }

        var updatedStation = await _stationService.UpdateAvailableSlotsAsync(id, request);
        if (updatedStation == null)
            return NotFound(new { Message = "Station not found or invalid slots value" });

        return Ok(updatedStation);
    }

    /// <summary>
    /// Update station schedule (Operator or Backoffice)
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="request">Schedule update request</param>
    /// <returns>Updated station details</returns>
    [HttpPatch("{id}/schedule")]
    [Authorize(Policy = "BackofficeOrOperator")]
    public async Task<IActionResult> UpdateSchedule(string id, [FromBody] UpdateStationScheduleRequest request)
    {
        // If operator, check if they operate this station
        if (HasRole("operator"))
        {
            var station = await _stationService.GetStationByIdAsync(id);
            if (station == null || station.OperatorId != GetCurrentUserId())
                return Forbid();
        }

        var updatedStation = await _stationService.UpdateScheduleAsync(id, request);
        if (updatedStation == null)
            return NotFound(new { Message = "Station not found or invalid schedule" });

        return Ok(updatedStation);
    }

    /// <summary>
    /// Assign operator to station (Backoffice only)
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="request">Operator assignment request</param>
    /// <returns>Updated station details</returns>
    [HttpPatch("{id}/operator")]
    [Authorize(Policy = "BackofficeOnly")]
    public async Task<IActionResult> AssignOperator(string id, [FromBody] AssignOperatorRequest request)
    {
        var updatedStation = await _stationService.AssignOperatorAsync(id, request);
        if (updatedStation == null)
            return NotFound(new { Message = "Station not found or invalid operator ID" });

        return Ok(updatedStation);
    }

    /// <summary>
    /// Activate or deactivate a station (Backoffice only)
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Updated station details</returns>
    [HttpPatch("{id}/status")]
    [Authorize(Policy = "BackofficeOnly")]
    public async Task<IActionResult> SetStationStatus(string id, [FromBody] UpdateStationStatusRequest request)
    {
        // Check if can deactivate (no active bookings)
        if (!request.IsActive)
        {
            var canDeactivate = await _stationService.CanDeactivateStationAsync(id);
            if (!canDeactivate)
                return BadRequest(new { Message = "Cannot deactivate station with active bookings" });
        }

        var updatedStation = await _stationService.SetStationStatusAsync(id, request);
        if (updatedStation == null)
            return NotFound(new { Message = "Station not found" });

        return Ok(updatedStation);
    }

    /// <summary>
    /// Delete a station (Backoffice only)
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "BackofficeOnly")]
    public async Task<IActionResult> DeleteStation(string id)
    {
        var canDelete = await _stationService.CanDeactivateStationAsync(id);
        if (!canDelete)
            return BadRequest(new { Message = "Cannot delete station with active bookings" });

        var success = await _stationService.DeleteStationAsync(id);
        if (!success)
            return NotFound(new { Message = "Station not found" });

        return NoContent();
    }
}