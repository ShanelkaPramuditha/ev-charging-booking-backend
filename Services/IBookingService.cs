using EadChargingBookingBackend.Models;
using EadChargingBookingBackend.DTOs;

namespace EadChargingBookingBackend.Services;

public interface IBookingService
{
    // Basic CRUD operations
    Task<IEnumerable<BookingResponse>> GetAllBookingsAsync();
    Task<BookingResponse?> GetBookingByIdAsync(string id);
    Task<BookingResponse?> CreateBookingAsync(string evOwnerId, CreateBookingRequest request);
    Task<BookingResponse?> UpdateBookingAsync(string id, string userId, UpdateBookingRequest request);
    Task<bool> DeleteBookingAsync(string id);

    // Specialized operations
    Task<IEnumerable<BookingResponse>> GetBookingsForEVOwnerAsync(string evOwnerId);
    Task<IEnumerable<BookingResponse>> GetBookingsForStationAsync(string stationId);
    Task<IEnumerable<BookingResponse>> GetUpcomingBookingsForEVOwnerAsync(string evOwnerId);
    Task<IEnumerable<BookingResponse>> GetBookingHistoryForEVOwnerAsync(string evOwnerId);
    Task<IEnumerable<BookingResponse>> GetActiveBookingsForStationAsync(string stationId);
    Task<BookingResponse?> CancelBookingAsync(string id, string userId, CancelBookingRequest request);
    Task<BookingResponse?> ApproveBookingAsync(string id, string operatorId);
    Task<BookingResponse?> CompleteBookingAsync(string id, string operatorId, CompleteBookingRequest request);
    Task<BookingQRResponse?> GenerateQRCodeAsync(string id);
    Task<QRValidationResponse> ValidateQRCodeAsync(QRValidationRequest request);
    Task<BookingCountResponse> GetBookingCountsForEVOwnerAsync(string evOwnerId);
    Task<BookingCountResponse> GetBookingCountsForStationAsync(string stationId);
    Task<bool> CanModifyBookingAsync(string id);
}