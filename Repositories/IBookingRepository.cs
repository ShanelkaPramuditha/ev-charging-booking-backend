/*
 * File Name: IBookingRepository.cs
 * Description: Interface for booking data repository operations.
 */

using EadChargingBookingBackend.Models;

namespace EadChargingBookingBackend.Repositories;

public interface IBookingRepository
{
    // Basic CRUD operations
    Task<IEnumerable<Booking>> GetAllAsync();
    Task<Booking?> GetByIdAsync(string id);
    Task<Booking> CreateAsync(
        string evOwnerId, string stationId, DateTime bookingDate,
        TimeSlot timeSlot, string vehicleInfo = "", string notes = "");
    Task<bool> UpdateAsync(Booking booking);
    Task<bool> DeleteAsync(string id);

    // Specialized operations
    Task<IEnumerable<Booking>> GetByEVOwnerIdAsync(string evOwnerId);
    Task<IEnumerable<Booking>> GetByStationIdAsync(string stationId);
    Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status);
    Task<IEnumerable<Booking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Booking>> GetActiveBookingsAsync();
    Task<IEnumerable<Booking>> GetUpcomingBookingsForEVOwnerAsync(string evOwnerId);
    Task<IEnumerable<Booking>> GetBookingHistoryForEVOwnerAsync(string evOwnerId);
    Task<IEnumerable<Booking>> GetActiveBookingsForStationAsync(string stationId);
    Task<bool> HasOverlappingBookingAsync(string stationId, DateTime bookingDate, TimeSlot timeSlot, string? excludeBookingId = null);
    Task<bool> UpdateStatusAsync(string id, BookingStatus status, string? cancellationReason = null);
    Task<bool> SetQRCodeAsync(string id, string qrCode);
    Task<Booking?> GetByQRCodeAsync(string qrCode);
    Task<int> GetPendingCountForUserAsync(string userId);
    Task<int> GetApprovedCountForUserAsync(string userId);
    Task<BookingStatus> GetBookingStatusAsync(string id);
}