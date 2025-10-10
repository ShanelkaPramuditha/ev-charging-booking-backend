/*
 * File Name: ChargingStation.cs
 * Description: Domain model for charging stations and related types.
 */

using MongoDB.Bson.Serialization.Attributes;

namespace EadChargingBookingBackend.Models;

public record ChargingStation : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("location")]
    public GeoLocation Location { get; init; } = new();

    [BsonElement("type")]
    public string Type { get; init; } = string.Empty; // e.g., "Fast", "Standard", "Ultra-fast"

    [BsonElement("totalSlots")]
    public int TotalSlots { get; init; }

    [BsonElement("availableSlots")]
    public int AvailableSlots { get; init; }

    [BsonElement("operatorId")]
    public string? OperatorId { get; init; }

    [BsonElement("schedule")]
    public List<ScheduleItem> Schedule { get; init; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; init; } = true;

    [BsonElement("address")]
    public string Address { get; init; } = string.Empty;

    [BsonElement("contactPhone")]
    public string ContactPhone { get; init; } = string.Empty;

    // Constructor for creating new charging stations
    public ChargingStation(string name, GeoLocation location, string type, int totalSlots,
        string? operatorId = null, string address = "", string contactPhone = "") : base()
    {
        Name = name;
        Location = location;
        Type = type;
        TotalSlots = totalSlots;
        AvailableSlots = totalSlots; // Initially all slots are available
        OperatorId = operatorId;
        Address = address;
        ContactPhone = contactPhone;
    }

    // Constructor with all properties (for MongoDB deserialization)
    public ChargingStation(string? id, string name, GeoLocation location, string type,
        int totalSlots, int availableSlots, string? operatorId, List<ScheduleItem> schedule,
        bool isActive, string address, string contactPhone,
        DateTime createdAt, DateTime updatedAt)
        : base(id, createdAt, updatedAt)
    {
        Name = name;
        Location = location;
        Type = type;
        TotalSlots = totalSlots;
        AvailableSlots = availableSlots;
        OperatorId = operatorId;
        Schedule = schedule;
        IsActive = isActive;
        Address = address;
        ContactPhone = contactPhone;
    }

    // Parameterless constructor for MongoDB
    public ChargingStation() : base()
    {
    }

    // Create a new station instance with updated properties
    public ChargingStation WithUpdatedInfo(string name, GeoLocation location, string type,
        int totalSlots, string address, string contactPhone)
    {
        return this with
        {
            Name = name,
            Location = location,
            Type = type,
            TotalSlots = totalSlots,
            // Keep available slots proportional if total changes
            AvailableSlots = TotalSlots > 0
                ? (int)Math.Ceiling((double)AvailableSlots / TotalSlots * totalSlots)
                : totalSlots,
            Address = address,
            ContactPhone = contactPhone,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Update available slots
    public ChargingStation WithAvailableSlots(int availableSlots)
    {
        return this with
        {
            AvailableSlots = Math.Min(Math.Max(0, availableSlots), TotalSlots),
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Update schedule
    public ChargingStation WithSchedule(List<ScheduleItem> schedule)
    {
        return this with
        {
            Schedule = schedule,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Assign an operator
    public ChargingStation WithOperator(string operatorId)
    {
        return this with
        {
            OperatorId = operatorId,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Activate/deactivate the station
    public ChargingStation WithStatus(bool isActive)
    {
        return this with
        {
            IsActive = isActive,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

// Represents a geographical coordinate
public record GeoLocation
{
    [BsonElement("latitude")]
    public double Latitude { get; init; }

    [BsonElement("longitude")]
    public double Longitude { get; init; }

    public GeoLocation(double latitude = 0, double longitude = 0)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}

// Represents a time slot in the station's schedule
public record ScheduleItem
{
    [BsonElement("dayOfWeek")]
    public DayOfWeek DayOfWeek { get; init; }

    [BsonElement("openTime")]
    public TimeSpan OpenTime { get; init; }

    [BsonElement("closeTime")]
    public TimeSpan CloseTime { get; init; }

    [BsonElement("isOpen")]
    public bool IsOpen { get; init; } = true;

    public ScheduleItem(DayOfWeek dayOfWeek, TimeSpan openTime, TimeSpan closeTime, bool isOpen = true)
    {
        DayOfWeek = dayOfWeek;
        OpenTime = openTime;
        CloseTime = closeTime;
        IsOpen = isOpen;
    }

    // Default constructor for MongoDB
    public ScheduleItem() { }
}