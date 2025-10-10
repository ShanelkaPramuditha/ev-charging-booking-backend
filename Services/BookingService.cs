/*
 * File Name: BookingService.cs
 * Description: Business logic for booking operations and lifecycle management.
 */

using EadChargingBookingBackend.DTOs;
using EadChargingBookingBackend.Models;
using EadChargingBookingBackend.Repositories;

namespace EadChargingBookingBackend.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IChargingStationRepository _stationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IQRCodeService _qrCodeService;

    public BookingService(
        IBookingRepository bookingRepository,
        IChargingStationRepository stationRepository,
        IUserRepository userRepository,
        IQRCodeService qrCodeService)
    {
        _bookingRepository = bookingRepository;
        _stationRepository = stationRepository;
        _userRepository = userRepository;
        _qrCodeService = qrCodeService;
    }

    public async Task<IEnumerable<BookingResponse>> GetAllBookingsAsync()
    {
        var bookings = await _bookingRepository.GetAllAsync();
        return await MapToResponsesAsync(bookings);
    }

    public async Task<BookingResponse?> GetBookingByIdAsync(string id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null)
            return null;

        return await MapToResponseAsync(booking);
    }

    public async Task<BookingResponse?> CreateBookingAsync(string evOwnerId, CreateBookingRequest request)
    {
        // Check if station exists and is active
        var station = await _stationRepository.GetByIdAsync(request.StationId);
        if (station == null || !station.IsActive)
            return null;

        // Check if the booking date is in the future and within 7 days
        var now = DateTime.UtcNow;
        var bookingDateTime = request.BookingDate.Date + request.TimeSlot.ToModel().StartTime;

        if (bookingDateTime <= now)
            return null; // Can't book in the past

        if (bookingDateTime > now.AddDays(7))
            return null; // Can't book more than 7 days ahead

        // Check if the station has available slots
        if (station.AvailableSlots <= 0)
            return null;

        // Check for overlapping bookings
        if (await _bookingRepository.HasOverlappingBookingAsync(
            request.StationId, request.BookingDate, request.TimeSlot.ToModel()))
            return null;

        // Create the booking
        var booking = await _bookingRepository.CreateAsync(
            evOwnerId,
            request.StationId,
            request.BookingDate,
            request.TimeSlot.ToModel(),
            request.VehicleInfo,
            request.Notes
        );

        // Update station available slots
        await _stationRepository.UpdateAvailableSlotsAsync(
            request.StationId, station.AvailableSlots - 1);

        return await MapToResponseAsync(booking);
    }

    public async Task<BookingResponse?> UpdateBookingAsync(string id, string userId, UpdateBookingRequest request)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null)
            return null;

        // Only the EV owner can modify their own booking
        if (booking.EVOwnerId != userId)
            return null;

        // Check if the booking can be modified (status, time limits)
        if (!booking.CanBeModified())
            return null;

        // Check if the booking date is in the future and within 7 days
        var now = DateTime.UtcNow;
        var bookingDateTime = request.BookingDate.Date + request.TimeSlot.ToModel().StartTime;

        if (bookingDateTime <= now)
            return null; // Can't book in the past

        if (bookingDateTime > now.AddDays(7))
            return null; // Can't book more than 7 days ahead

        // Check for overlapping bookings
        if (await _bookingRepository.HasOverlappingBookingAsync(
            booking.StationId, request.BookingDate, request.TimeSlot.ToModel(), booking.Id))
            return null;

        // Update the booking
        var updatedBooking = booking.WithUpdatedDetails(
            request.BookingDate,
            request.TimeSlot.ToModel(),
            request.VehicleInfo,
            request.Notes
        );

        // If it was approved, generate a new QR code
        if (booking.Status == BookingStatus.Approved)
        {
            var qrCode = _qrCodeService.GenerateQRCode(
                booking.Id!,
                booking.EVOwnerId,
                booking.StationId,
                request.BookingDate
            );
            updatedBooking = updatedBooking.WithQRCode(qrCode);
        }

        if (await _bookingRepository.UpdateAsync(updatedBooking))
            return await MapToResponseAsync(updatedBooking);

        return null;
    }

    public async Task<bool> DeleteBookingAsync(string id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null)
            return false;

        // If booking was approved, free up a slot
        if (booking.Status == BookingStatus.Approved)
        {
            var station = await _stationRepository.GetByIdAsync(booking.StationId);
            if (station != null)
            {
                await _stationRepository.UpdateAvailableSlotsAsync(
                    booking.StationId, station.AvailableSlots + 1);
            }
        }

        return await _bookingRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<BookingResponse>> GetBookingsForEVOwnerAsync(string evOwnerId)
    {
        var bookings = await _bookingRepository.GetByEVOwnerIdAsync(evOwnerId);
        return await MapToResponsesAsync(bookings);
    }

    public async Task<IEnumerable<BookingResponse>> GetBookingsForStationAsync(string stationId)
    {
        var bookings = await _bookingRepository.GetByStationIdAsync(stationId);
        return await MapToResponsesAsync(bookings);
    }

    public async Task<IEnumerable<BookingResponse>> GetUpcomingBookingsForEVOwnerAsync(string evOwnerId)
    {
        var bookings = await _bookingRepository.GetUpcomingBookingsForEVOwnerAsync(evOwnerId);
        return await MapToResponsesAsync(bookings);
    }

    public async Task<IEnumerable<BookingResponse>> GetBookingHistoryForEVOwnerAsync(string evOwnerId)
    {
        var bookings = await _bookingRepository.GetBookingHistoryForEVOwnerAsync(evOwnerId);
        return await MapToResponsesAsync(bookings);
    }

    public async Task<IEnumerable<BookingResponse>> GetActiveBookingsForStationAsync(string stationId)
    {
        var bookings = await _bookingRepository.GetActiveBookingsForStationAsync(stationId);
        return await MapToResponsesAsync(bookings);
    }

    public async Task<BookingResponse?> CancelBookingAsync(string id, string userId, CancelBookingRequest request)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null)
            return null;

        // Check if user is authorized to cancel the booking
        var isEVOwner = booking.EVOwnerId == userId;
        var isOperator = await IsOperatorForStationAsync(userId, booking.StationId);
        var isBackoffice = await IsBackofficeUserAsync(userId);

        if (!isEVOwner && !isOperator && !isBackoffice)
            return null;

        // Check if the booking can be cancelled (EV owners have 12h limit)
        if (isEVOwner && !booking.CanBeCancelled())
            return null;

        // Update booking status
        var updatedBooking = booking.WithStatus(BookingStatus.Cancelled, request.CancellationReason);

        if (await _bookingRepository.UpdateAsync(updatedBooking))
        {
            // Free up a slot if booking was approved
            if (booking.Status == BookingStatus.Approved)
            {
                var station = await _stationRepository.GetByIdAsync(booking.StationId);
                if (station != null)
                {
                    await _stationRepository.UpdateAvailableSlotsAsync(
                        booking.StationId, station.AvailableSlots + 1);
                }
            }

            return await MapToResponseAsync(updatedBooking);
        }

        return null;
    }

    public async Task<BookingResponse?> ApproveBookingAsync(string id, string operatorId)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null)
            return null;

        // Check if user is authorized to approve the booking
        var isOperator = await IsOperatorForStationAsync(operatorId, booking.StationId);
        var isBackoffice = await IsBackofficeUserAsync(operatorId);

        if (!isOperator && !isBackoffice)
            return null;

        // Can only approve pending bookings
        if (booking.Status != BookingStatus.Pending)
            return null;

        // Update status to approved
        var updatedBooking = booking.WithStatus(BookingStatus.Approved);

        // Generate QR code
        var qrCode = _qrCodeService.GenerateQRCode(
            booking.Id!,
            booking.EVOwnerId,
            booking.StationId,
            booking.BookingDate
        );

        updatedBooking = updatedBooking.WithQRCode(qrCode);

        if (await _bookingRepository.UpdateAsync(updatedBooking))
            return await MapToResponseAsync(updatedBooking);

        return null;
    }

    public async Task<BookingResponse?> CompleteBookingAsync(string id, string operatorId, CompleteBookingRequest request)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null)
            return null;

        // Check if user is authorized to complete the booking
        var isOperator = await IsOperatorForStationAsync(operatorId, booking.StationId);
        var isBackoffice = await IsBackofficeUserAsync(operatorId);

        if (!isOperator && !isBackoffice)
            return null;

        // Can only complete approved bookings
        if (booking.Status != BookingStatus.Approved)
            return null;

        BookingStatus newStatus;
        if (request.Completed)
        {
            newStatus = BookingStatus.Completed;
        }
        else
        {
            newStatus = BookingStatus.NoShow;
        }

        // Update booking status
        var updatedBooking = booking.WithStatus(newStatus);

        if (await _bookingRepository.UpdateAsync(updatedBooking))
        {
            // Free up a slot
            var station = await _stationRepository.GetByIdAsync(booking.StationId);
            if (station != null)
            {
                await _stationRepository.UpdateAvailableSlotsAsync(
                    booking.StationId, station.AvailableSlots + 1);
            }

            return await MapToResponseAsync(updatedBooking);
        }

        return null;
    }

    public async Task<BookingQRResponse?> GenerateQRCodeAsync(string id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null)
            return null;

        // Only approved bookings have QR codes
        if (booking.Status != BookingStatus.Approved)
            return null;

        // If QR code already exists, return it
        if (!string.IsNullOrEmpty(booking.QRCode))
        {
            var station = await _stationRepository.GetByIdAsync(booking.StationId);

            return new BookingQRResponse(
                booking.Id!,
                booking.QRCode,
                station?.Name ?? "Unknown Station",
                station?.Address ?? "Unknown Address",
                booking.BookingDate,
                TimeSlotDto.FromModel(booking.TimeSlot)
            );
        }

        // Generate new QR code
        var qrCode = _qrCodeService.GenerateQRCode(
            booking.Id!,
            booking.EVOwnerId,
            booking.StationId,
            booking.BookingDate
        );

        var updatedBooking = booking.WithQRCode(qrCode);

        if (await _bookingRepository.UpdateAsync(updatedBooking))
        {
            var station = await _stationRepository.GetByIdAsync(booking.StationId);

            return new BookingQRResponse(
                updatedBooking.Id!,
                updatedBooking.QRCode,
                station?.Name ?? "Unknown Station",
                station?.Address ?? "Unknown Address",
                updatedBooking.BookingDate,
                TimeSlotDto.FromModel(updatedBooking.TimeSlot)
            );
        }

        return null;
    }

    public async Task<QRValidationResponse> ValidateQRCodeAsync(QRValidationRequest request)
    {
        // Find booking by QR code
        var booking = await _bookingRepository.GetByQRCodeAsync(request.QRCode);
        if (booking == null)
            return new QRValidationResponse(false);

        // Check if booking is approved
        if (booking.Status != BookingStatus.Approved)
            return new QRValidationResponse(false);

        // Validate QR code
        var isValid = _qrCodeService.ValidateQRCode(
            request.QRCode,
            booking.Id!,
            booking.EVOwnerId,
            booking.StationId
        );

        if (isValid)
        {
            return new QRValidationResponse(
                true,
                await MapToResponseAsync(booking)
            );
        }

        return new QRValidationResponse(false);
    }

    public async Task<BookingCountResponse> GetBookingCountsForEVOwnerAsync(string evOwnerId)
    {
        var now = DateTime.UtcNow;
        var allBookings = await _bookingRepository.GetByEVOwnerIdAsync(evOwnerId);

        int pendingCount = 0;
        int approvedCount = 0;
        int todayCount = 0;
        int cancelledCount = 0;
        int completedCount = 0;

        foreach (var booking in allBookings)
        {
            if (booking.Status == BookingStatus.Pending)
                pendingCount++;

            if (booking.Status == BookingStatus.Approved)
            {
                approvedCount++;
                if (booking.BookingDate.Date == now.Date)
                    todayCount++;
            }

            if (booking.Status == BookingStatus.Cancelled)
                cancelledCount++;

            if (booking.Status == BookingStatus.Completed)
                completedCount++;
        }

        return new BookingCountResponse(
            pendingCount,
            approvedCount,
            todayCount,
            cancelledCount,
            completedCount,
            allBookings.Count()
        );
    }

    public async Task<BookingCountResponse> GetBookingCountsForStationAsync(string stationId)
    {
        var now = DateTime.UtcNow;
        var allBookings = await _bookingRepository.GetByStationIdAsync(stationId);

        int pendingCount = 0;
        int approvedCount = 0;
        int todayCount = 0;
        int cancelledCount = 0;
        int completedCount = 0;

        foreach (var booking in allBookings)
        {
            if (booking.Status == BookingStatus.Pending)
                pendingCount++;

            if (booking.Status == BookingStatus.Approved)
            {
                approvedCount++;
                if (booking.BookingDate.Date == now.Date)
                    todayCount++;
            }

            if (booking.Status == BookingStatus.Cancelled)
                cancelledCount++;

            if (booking.Status == BookingStatus.Completed)
                completedCount++;
        }

        return new BookingCountResponse(
            pendingCount,
            approvedCount,
            todayCount,
            cancelledCount,
            completedCount,
            allBookings.Count()
        );
    }

    public async Task<bool> CanModifyBookingAsync(string id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        return booking?.CanBeModified() ?? false;
    }

    // Helper methods
    private async Task<BookingResponse> MapToResponseAsync(Booking booking)
    {
        // Get EV owner info
        var evOwner = await _userRepository.GetByIdAsync(booking.EVOwnerId);

        // Get station info
        var station = await _stationRepository.GetByIdAsync(booking.StationId);

        return new BookingResponse(
            booking.Id!,
            booking.EVOwnerId,
            evOwner?.Username ?? "Unknown User",
            booking.StationId,
            station?.Name ?? "Unknown Station",
            station?.Address ?? "Unknown Address",
            booking.BookingDate,
            TimeSlotDto.FromModel(booking.TimeSlot),
            booking.Status,
            booking.QRCode,
            booking.VehicleInfo,
            booking.Notes,
            booking.CancellationReason,
            booking.CreatedAt,
            booking.UpdatedAt
        );
    }

    private async Task<IEnumerable<BookingResponse>> MapToResponsesAsync(IEnumerable<Booking> bookings)
    {
        var responses = new List<BookingResponse>();
        foreach (var booking in bookings)
        {
            responses.Add(await MapToResponseAsync(booking));
        }
        return responses;
    }

    private async Task<bool> IsOperatorForStationAsync(string userId, string stationId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.Role != "operator")
            return false;

        var station = await _stationRepository.GetByIdAsync(stationId);
        return station?.OperatorId == userId;
    }

    private async Task<bool> IsBackofficeUserAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Role == "backOffice";
    }
}
