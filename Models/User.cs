using MongoDB.Bson.Serialization.Attributes;

namespace EadChargingBookingBackend.Models;

public record User : BaseEntity
{
    [BsonElement("username")]
    public string Username { get; init; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; init; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; init; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; init; } = string.Empty;

    [BsonElement("nic")]
    public string NIC { get; init; } = string.Empty; // Use empty string instead of null to avoid index issues

    [BsonElement("isActive")]
    public bool IsActive { get; init; } = true;

    // Constructor for creating new users
    public User(string username, string email, string passwordHash, string role, string nic = "") : base()
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        NIC = nic;
    }

    // Constructor with all properties (for MongoDB deserialization)
    public User(string? id, string username, string email, string passwordHash, string role, string nic, bool isActive, DateTime createdAt, DateTime updatedAt)
        : base(id, createdAt, updatedAt)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        NIC = nic;
        IsActive = isActive;
    }

    // Parameterless constructor for MongoDB
    public User() : base()
    {
    }
}