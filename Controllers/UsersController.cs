using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EadChargingBookingBackend.Services;
using EadChargingBookingBackend.DTOs;

namespace EadChargingBookingBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all endpoints
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get all users with optional filters (Office User only)
    /// </summary>
    /// <param name="role">Optional: User role filter (backOffice, operator, evOwner)</param>
    /// <param name="search">Optional: Search term to filter by username, email, or NIC</param>
    /// <param name="status">Optional: User status filter (active, inactive)</param>
    /// <returns>List of users matching the filters</returns>
    [HttpGet]
    [Authorize(Policy = "BackOfficeOnly")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? role = null,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        // Validate role if provided
        if (!string.IsNullOrEmpty(role))
        {
            if (role != "backOffice" && role != "operator" && role != "evOwner")
                return BadRequest(new { Message = "Invalid role. Must be 'backOffice', 'operator', or 'evOwner'" });
        }

        // Parse status parameter
        bool? isActive = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
                isActive = true;
            else if (status.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                isActive = false;
            else
                return BadRequest(new { Message = "Invalid status. Must be 'active' or 'inactive'" });
        }

        // Get filtered users
        var users = await _userService.GetFilteredUsersAsync(role, search, isActive);
        return Ok(users);
    }

    /// <summary>
    /// Create a new user (Office User only)
    /// </summary>
    /// <param name="request">Create user request</param>
    /// <returns>Created user details</returns>
    [HttpPost]
    [Authorize(Policy = "BackOfficeOnly")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // Validate request
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!request.IsValidRole())
            return BadRequest(new { Message = "Invalid role. Must be 'backOffice', 'operator', or 'evOwner'" });

        // Validate NIC requirement for EVOwner
        if (!request.IsValidNIC())
            return BadRequest(new { Message = "NIC is required for EVOwner role" });

        var user = await _userService.CreateUserAsync(request);
        if (user == null)
            return BadRequest(new { Message = "User creation failed. Username, email, or NIC may already exist." });

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound(new { Message = "User not found" });

        return Ok(user);
    }

    /// <summary>
    /// Update user information (Office User only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated user details</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "BackOfficeOnly")]
    public async Task<IActionResult> UpdateUser(string id, UpdateUserRequest request)
    {
        var updatedUser = await _userService.UpdateUserAsync(id, request);
        if (updatedUser == null)
            return BadRequest(new { Message = "User update failed. Check if the user exists and provided data is valid." });

        return Ok(updatedUser);
    }

    /// <summary>
    /// Update user status (activate/deactivate) (Office User only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Update status request</param>
    /// <returns>Updated user details</returns>
    [HttpPatch("{id}/status")]
    [Authorize(Policy = "BackOfficeOnly")]
    public async Task<IActionResult> UpdateUserStatus(string id, UpdateUserStatusRequest request)
    {
        var updatedUser = await _userService.UpdateUserStatusAsync(id, request.IsActive);
        if (updatedUser == null)
            return NotFound(new { Message = "User not found" });

        return Ok(updatedUser);
    }

    /// <summary>
    /// Activate a user account (Office User only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Updated user details</returns>
    [HttpPatch("{id}/activate")]
    [Authorize(Policy = "BackOfficeOnly")]
    public async Task<IActionResult> ActivateUser(string id)
    {
        var updatedUser = await _userService.UpdateUserStatusAsync(id, true);
        if (updatedUser == null)
            return NotFound(new { Message = "User not found" });

        return Ok(updatedUser);
    }

    /// <summary>
    /// Deactivate a user account (Office User only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Updated user details</returns>
    [HttpPatch("{id}/deactivate")]
    [Authorize(Policy = "BackOfficeOnly")]
    public async Task<IActionResult> DeactivateUser(string id)
    {
        var updatedUser = await _userService.UpdateUserStatusAsync(id, false);
        if (updatedUser == null)
            return NotFound(new { Message = "User not found" });

        return Ok(updatedUser);
    }

    /// <summary>
    /// Delete user (Office User only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "BackOfficeOnly")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (!success)
            return NotFound(new { Message = "User not found" });

        return NoContent();
    }
}