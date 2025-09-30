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

    [BsonElement("isActive")]
    public bool IsActive { get; init; } = true;

    // Constructor for creating new users
    public User(string username, string email, string passwordHash, string role) : base()
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    // Constructor with all properties (for MongoDB deserialization)
    public User(string? id, string username, string email, string passwordHash, string role, bool isActive, DateTime createdAt, DateTime updatedAt)
        : base(id, createdAt, updatedAt)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = isActive;
    }

    // Parameterless constructor for MongoDB
    public User() : base()
    {
    }
}