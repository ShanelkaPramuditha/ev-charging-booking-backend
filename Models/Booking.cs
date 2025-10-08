using MongoDB.Bson.Serialization.Attributes;

namespace EadChargingBookingBackend.Models;

public record Booking : BaseEntity
{
    [BsonElement("evOwnerId")]
    public string EVOwnerId { get; init; } = string.Empty;

    [BsonElement("stationId")]
    public string StationId { get; init; } = string.Empty;

    [BsonElement("bookingDate")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime BookingDate { get; init; }

    [BsonElement("timeSlot")]
    public TimeSlot TimeSlot { get; init; } = new();

    [BsonElement("status")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public BookingStatus Status { get; init; } = BookingStatus.Pending;

    [BsonElement("qrCode")]
    public string QRCode { get; init; } = string.Empty;

    [BsonElement("vehicleInfo")]
    public string VehicleInfo { get; init; } = string.Empty;

    [BsonElement("notes")]
    public string Notes { get; init; } = string.Empty;

    [BsonElement("cancellationReason")]
    public string CancellationReason { get; init; } = string.Empty;

    // Constructor for creating new bookings
    public Booking(string evOwnerId, string stationId, DateTime bookingDate, TimeSlot timeSlot,
        string vehicleInfo = "", string notes = "") : base()
    {
        EVOwnerId = evOwnerId;
        StationId = stationId;
        BookingDate = bookingDate.Date; // Store date part only
        TimeSlot = timeSlot;
        Status = BookingStatus.Pending;
        VehicleInfo = vehicleInfo;
        Notes = notes;
    }

    // Constructor with all properties (for MongoDB deserialization)
    public Booking(string? id, string evOwnerId, string stationId, DateTime bookingDate,
        TimeSlot timeSlot, BookingStatus status, string qrCode, string vehicleInfo,
        string notes, string cancellationReason, DateTime createdAt, DateTime updatedAt)
        : base(id, createdAt, updatedAt)
    {
        EVOwnerId = evOwnerId;
        StationId = stationId;
        BookingDate = bookingDate;
        TimeSlot = timeSlot;
        Status = status;
        QRCode = qrCode;
        VehicleInfo = vehicleInfo;
        Notes = notes;
        CancellationReason = cancellationReason;
    }

    // Parameterless constructor for MongoDB
    public Booking() : base()
    {
    }

    // Update booking details
    public Booking WithUpdatedDetails(DateTime bookingDate, TimeSlot timeSlot, string vehicleInfo, string notes)
    {
        return this with
        {
            BookingDate = bookingDate.Date,
            TimeSlot = timeSlot,
            VehicleInfo = vehicleInfo,
            Notes = notes,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Update booking status
    public Booking WithStatus(BookingStatus status, string? cancellationReason = null)
    {
        return this with
        {
            Status = status,
            CancellationReason = status == BookingStatus.Cancelled
                ? (cancellationReason ?? CancellationReason)
                : string.Empty,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Set QR code
    public Booking WithQRCode(string qrCode)
    {
        return this with
        {
            QRCode = qrCode,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Check if booking can be modified
    public bool CanBeModified()
    {
        // Can only modify pending bookings
        if (Status != BookingStatus.Pending && Status != BookingStatus.Approved)
            return false;

        // Must be at least 12 hours before the booking
        return (BookingDate.Date + TimeSlot.StartTime) > DateTime.UtcNow.AddHours(12);
    }

    // Check if booking can be cancelled
    public bool CanBeCancelled()
    {
        // Can only cancel pending or approved bookings
        if (Status != BookingStatus.Pending && Status != BookingStatus.Approved)
            return false;

        // Must be at least 12 hours before the booking
        return (BookingDate.Date + TimeSlot.StartTime) > DateTime.UtcNow.AddHours(12);
    }
}

// Time slot for a booking
public record TimeSlot
{
    [BsonElement("startTime")]
    public TimeSpan StartTime { get; init; }

    [BsonElement("endTime")]
    public TimeSpan EndTime { get; init; }

    public TimeSlot(TimeSpan startTime, TimeSpan endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }

    // Default constructor for MongoDB
    public TimeSlot() { }

    // Duration of the time slot
    public TimeSpan Duration => EndTime - StartTime;

    // Check if time slots overlap
    public bool OverlapsWith(TimeSlot other)
    {
        return (StartTime < other.EndTime && EndTime > other.StartTime);
    }
}

// Enum for booking status
public enum BookingStatus
{
    Pending,    // Initial state, awaiting approval
    Approved,   // Booking approved, can be used
    Cancelled,  // Booking cancelled by user or system
    Completed,  // Charging session completed
    NoShow      // User didn't show up
}