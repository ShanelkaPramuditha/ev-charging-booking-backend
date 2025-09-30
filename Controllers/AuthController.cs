using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EadChargingBookingBackend.Services;
using EadChargingBookingBackend.DTOs;
using System.ComponentModel.DataAnnotations;

namespace EadChargingBookingBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validate request
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);

        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            var errors = validationResults.Select(vr => vr.ErrorMessage);
            return BadRequest(new { Errors = errors });
        }

        if (!request.IsValidRole())
            return BadRequest(new { Message = "Invalid role. Must be 'officeUser' or 'operator'" });

        var response = await _userService.RegisterAsync(request);
        if (response == null)
            return BadRequest(new { Message = "Username or email already exists" });

        return Ok(response);
    }

    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="request">Login request</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Validate request
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);

        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            var errors = validationResults.Select(vr => vr.ErrorMessage);
            return BadRequest(new { Errors = errors });
        }

        var response = await _userService.LoginAsync(request);
        if (response == null)
            return Unauthorized(new { Message = "Invalid username or password" });

        return Ok(response);
    }
}
