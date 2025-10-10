/*
 * File Name: BookingDTO.cs
 * Description: Data transfer objects for booking operations.
 */

using System.ComponentModel.DataAnnotations;
using EadChargingBookingBackend.Models;

namespace EadChargingBookingBackend.DTOs;

// DTOs for creating/updating bookings
public record CreateBookingRequest(
    [Required] string StationId,
    [Required] DateTime BookingDate,
    [Required] TimeSlotDto TimeSlot,
    string VehicleInfo = "",
    string Notes = ""
);

public record UpdateBookingRequest(
    [Required] DateTime BookingDate,
    [Required] TimeSlotDto TimeSlot,
    string VehicleInfo = "",
    string Notes = ""
);

public record CancelBookingRequest(
    [Required] string CancellationReason
);

public record ApproveBookingRequest(
    bool Approved = true
);

public record CompleteBookingRequest(
    [Required] bool Completed,
    string? Notes = null
);

// DTOs for booking responses
public record BookingResponse(
    string Id,
    string EVOwnerId,
    string EVOwnerName,
    string StationId,
    string StationName,
    string StationAddress,
    DateTime BookingDate,
    TimeSlotDto TimeSlot,
    BookingStatus Status,
    string QRCode,
    string VehicleInfo,
    string Notes,
    string CancellationReason,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record BookingSummaryResponse(
    string Id,
    string StationName,
    string StationAddress,
    DateTime BookingDate,
    TimeSlotDto TimeSlot,
    BookingStatus Status
);

public record BookingQRResponse(
    string Id,
    string QRCode,
    string StationName,
    string StationAddress,
    DateTime BookingDate,
    TimeSlotDto TimeSlot
);

public record QRValidationRequest(
    [Required] string QRCode
);

public record QRValidationResponse(
    bool IsValid,
    BookingResponse? Booking = null
);

public record BookingCountResponse(
    int PendingCount,
    int ApprovedCount,
    int TodayCount,
    int CancelledCount,
    int CompletedCount,
    int TotalCount
);

// Supporting DTOs
public record TimeSlotDto(
    [Required] string StartTime, // Format: "HH:MM"
    [Required] string EndTime    // Format: "HH:MM"
)
{
    public TimeSlot ToModel()
    {
        return new TimeSlot(
            TimeSpan.Parse(StartTime),
            TimeSpan.Parse(EndTime)
        );
    }

    public static TimeSlotDto FromModel(TimeSlot timeSlot)
    {
        return new TimeSlotDto(
            timeSlot.StartTime.ToString(@"hh\:mm"),
            timeSlot.EndTime.ToString(@"hh\:mm")
        );
    }
}