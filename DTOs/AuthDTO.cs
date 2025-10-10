/*
 * File Name: AuthDTO.cs
 * Description: Data transfer objects for authentication requests and responses.
 */

using System.ComponentModel.DataAnnotations;

namespace EadChargingBookingBackend.DTOs;

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
);

public record NICLoginRequest(
    [Required] string NIC,
    [Required] string Password
);

public record RegisterRequest(
    [Required] string Username,
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Password,
    [Required] string Role, // "backOffice", "operator", or "evOwner"
    string? NIC = null // Required only for EVOwner role
)
{
    public bool IsValidRole() => Role == "backOffice" || Role == "operator" || Role == "evOwner";

    public bool IsNICRequired() => Role == "evOwner";

    public bool IsValidNIC() => !IsNICRequired() || !string.IsNullOrWhiteSpace(NIC);
}

public record AuthResponse(
    string Token,
    string Username,
    string Email,
    string Role,
    DateTime ExpiresAt,
    UserResponse User
);

public record UserResponse(
    string Id,
    string Username,
    string Email,
    string Role,
    string? NIC,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateUserRequest(
    [Required] string Username,
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Password,
    [Required] string Role, // "backOffice", "operator", or "evOwner"
    string? NIC = null // Required only for EVOwner role
)
{
    public bool IsValidRole() => Role == "backOffice" || Role == "operator" || Role == "evOwner";

    public bool IsNICRequired() => Role == "evOwner";

    public bool IsValidNIC() => !IsNICRequired() || !string.IsNullOrWhiteSpace(NIC);
}
