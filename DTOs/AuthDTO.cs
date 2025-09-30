using System.ComponentModel.DataAnnotations;

namespace EadChargingBookingBackend.DTOs;

public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

public record RegisterRequest(
    [Required] string Username,
    [Required] [EmailAddress] string Email,
    [Required] [MinLength(6)] string Password,
    [Required] string Role // "officeUser" or "operator"
)
{
    public bool IsValidRole() => Role == "officeUser" || Role == "operator";
}

public record AuthResponse(
    string Token,
    string Username,
    string Email,
    string Role,
    DateTime ExpiresAt
);

public record UserResponse(
    string Id,
    string Username,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
