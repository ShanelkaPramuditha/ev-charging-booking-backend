using System.ComponentModel.DataAnnotations;

namespace EadChargingBookingBackend.DTOs;

public record UpdateUserRequest(
    [Required] string Username,
    [Required][EmailAddress] string Email,
    [Required] string Role, // "backOffice", "operator", or "evOwner"
    string? NIC = null, // Required only for EVOwner role
    bool? IsActive = null
)
{
    public bool IsValidRole() => Role == "backOffice" || Role == "operator" || Role == "evOwner";

    public bool IsNICRequired() => Role == "evOwner";

    public bool IsValidNIC() => !IsNICRequired() || !string.IsNullOrWhiteSpace(NIC);
}

// Request specifically for activating/deactivating users
public record UpdateUserStatusRequest(
    [Required] bool IsActive
);