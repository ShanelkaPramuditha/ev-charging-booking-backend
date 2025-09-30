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
    /// Get all users (Office User only)
    /// </summary>
    /// <returns>List of all users</returns>
    [HttpGet]
    [Authorize(Policy = "OfficeUserOnly")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
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
    /// Delete user (Office User only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "OfficeUserOnly")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (!success)
            return NotFound(new { Message = "User not found" });

        return NoContent();
    }
}