using System.ComponentModel.DataAnnotations;
using EadChargingBookingBackend.Models;

namespace EadChargingBookingBackend.DTOs;

// DTOs for creating/updating charging stations
public record CreateStationRequest(
    [Required] string Name,
    [Required] GeoLocationDto Location,
    [Required] string Type,
    [Required][Range(1, int.MaxValue)] int TotalSlots,
    string? OperatorId,
    [Required] string Address,
    string ContactPhone,
    List<ScheduleItemDto>? Schedule
);

public record UpdateStationRequest(
    [Required] string Name,
    [Required] GeoLocationDto Location,
    [Required] string Type,
    [Required][Range(1, int.MaxValue)] int TotalSlots,
    [Required] string Address,
    string ContactPhone
);

public record UpdateStationSlotsRequest(
    [Required][Range(0, int.MaxValue)] int AvailableSlots
);

public record UpdateStationScheduleRequest(
    [Required] List<ScheduleItemDto> Schedule
);

public record AssignOperatorRequest(
    [Required] string OperatorId
);

public record UpdateStationStatusRequest(
    [Required] bool IsActive
);

// DTOs for responses
public record StationResponse(
    string Id,
    string Name,
    GeoLocationDto Location,
    string Type,
    int TotalSlots,
    int AvailableSlots,
    string? OperatorId,
    string? OperatorName,
    List<ScheduleItemDto> Schedule,
    bool IsActive,
    string Address,
    string ContactPhone,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record StationSummaryResponse(
    string Id,
    string Name,
    string Type,
    int TotalSlots,
    int AvailableSlots,
    bool IsActive,
    string Address
);

public record NearbyStationResponse(
    string Id,
    string Name,
    GeoLocationDto Location,
    string Type,
    int AvailableSlots,
    double DistanceKm,
    string Address
);

// Supporting DTOs
public record GeoLocationDto(
    [Required][Range(-90, 90)] double Latitude,
    [Required][Range(-180, 180)] double Longitude
)
{
    public GeoLocation ToModel()
    {
        return new GeoLocation(Latitude, Longitude);
    }

    public static GeoLocationDto FromModel(GeoLocation location)
    {
        return new GeoLocationDto(location.Latitude, location.Longitude);
    }
}

public record ScheduleItemDto(
    [Required] DayOfWeek DayOfWeek,
    [Required] string OpenTime, // Format: "HH:MM"
    [Required] string CloseTime, // Format: "HH:MM"
    bool IsOpen = true
)
{
    public ScheduleItem ToModel()
    {
        return new ScheduleItem(
            DayOfWeek,
            TimeSpan.Parse(OpenTime),
            TimeSpan.Parse(CloseTime),
            IsOpen
        );
    }

    public static ScheduleItemDto FromModel(ScheduleItem schedule)
    {
        return new ScheduleItemDto(
            schedule.DayOfWeek,
            schedule.OpenTime.ToString(@"hh\:mm"),
            schedule.CloseTime.ToString(@"hh\:mm"),
            schedule.IsOpen
        );
    }
}