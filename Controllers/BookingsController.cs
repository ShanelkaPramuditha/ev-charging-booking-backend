/*
 * File Name: BookingsController.cs
 * Description: API controller for managing booking operations and lifecycle.
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EadChargingBookingBackend.Services;
using EadChargingBookingBackend.DTOs;
using System.Security.Claims;

namespace EadChargingBookingBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all endpoints
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IChargingStationService _stationService;

    public BookingsController(IBookingService bookingService, IChargingStationService stationService)
    {
        _bookingService = bookingService;
        _stationService = stationService;
    }

    // Helper to get current user ID from claims
    private string GetCurrentUserId() => User.FindFirstValue("userId") ?? string.Empty;

    // Helper to check if current user has specific role
    private bool HasRole(string role) => User.IsInRole(role);

    /// <summary>
    /// Get all bookings (Backoffice only)
    /// </summary>
    /// <returns>List of all bookings</returns>
    [HttpGet]
    [Authorize(Policy = "BackofficeOnly")]
    public async Task<IActionResult> GetAllBookings()
    {
        var bookings = await _bookingService.GetAllBookingsAsync();
        return Ok(bookings);
    }

    /// <summary>
    /// Get booking by ID (accessible by Backoffice, station operator, or the EV owner)
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Booking details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBooking(string id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
            return NotFound(new { Message = "Booking not found" });

        // Check authorization - only allow backoffice, the EV owner, or the station operator
        if (!HasRole("backOffice") &&
            GetCurrentUserId() != booking.EVOwnerId)
        {
            // Check if user is the operator for this station
            var station = await _stationService.GetStationByIdAsync(booking.StationId);
            if (station == null || station.OperatorId != GetCurrentUserId())
                return Forbid();
        }

        return Ok(booking);
    }

    /// <summary>
    /// Get bookings for current EV owner (EVOwner only)
    /// </summary>
    /// <returns>List of bookings for the authenticated EV owner</returns>
    [HttpGet("my-bookings")]
    [Authorize(Policy = "EVOwnerOnly")]
    public async Task<IActionResult> GetMyBookings()
    {
        var bookings = await _bookingService.GetBookingsForEVOwnerAsync(GetCurrentUserId());
        return Ok(bookings);
    }

    /// <summary>
    /// Get upcoming bookings for current EV owner (EVOwner only)
    /// </summary>
    /// <returns>List of upcoming bookings for the authenticated EV owner</returns>
    [HttpGet("my-upcoming")]
    [Authorize(Policy = "EVOwnerOnly")]
    public async Task<IActionResult> GetMyUpcomingBookings()
    {
        var bookings = await _bookingService.GetUpcomingBookingsForEVOwnerAsync(GetCurrentUserId());
        return Ok(bookings);
    }

    /// <summary>
    /// Get booking history for current EV owner (EVOwner only)
    /// </summary>
    /// <returns>List of past bookings for the authenticated EV owner</returns>
    [HttpGet("my-history")]
    [Authorize(Policy = "EVOwnerOnly")]
    public async Task<IActionResult> GetMyBookingHistory()
    {
        var bookings = await _bookingService.GetBookingHistoryForEVOwnerAsync(GetCurrentUserId());
        return Ok(bookings);
    }

    /// <summary>
    /// Get bookings for a specific EV owner (Backoffice only)
    /// </summary>
    /// <param name="evOwnerId">EV owner ID</param>
    /// <returns>List of bookings for the specified EV owner</returns>
    [HttpGet("ev-owner/{evOwnerId}")]
    [Authorize(Policy = "BackofficeOnly")]
    public async Task<IActionResult> GetBookingsByEVOwner(string evOwnerId)
    {
        var bookings = await _bookingService.GetBookingsForEVOwnerAsync(evOwnerId);
        return Ok(bookings);
    }

    /// <summary>
    /// Get bookings for a specific station (Backoffice or station operator)
    /// </summary>
    /// <param name="stationId">Station ID</param>
    /// <returns>List of bookings for the specified station</returns>
    [HttpGet("station/{stationId}")]
    [Authorize(Policy = "BackofficeOrOperator")]
    public async Task<IActionResult> GetBookingsByStation(string stationId)
    {
        // If operator, check if they operate this station
        if (HasRole("operator"))
        {
            var station = await _stationService.GetStationByIdAsync(stationId);
            if (station == null || station.OperatorId != GetCurrentUserId())
                return Forbid();
        }

        var bookings = await _bookingService.GetBookingsForStationAsync(stationId);
        return Ok(bookings);
    }

    /// <summary>
    /// Get active bookings for a specific station (Backoffice or station operator)
    /// </summary>
    /// <param name="stationId">Station ID</param>
    /// <returns>List of active bookings for the specified station</returns>
    [HttpGet("station/{stationId}/active")]
    [Authorize(Policy = "BackofficeOrOperator")]
    public async Task<IActionResult> GetActiveBookingsByStation(string stationId)
    {
        // If operator, check if they operate this station
        if (HasRole("operator"))
        {
            var station = await _stationService.GetStationByIdAsync(stationId);
            if (station == null || station.OperatorId != GetCurrentUserId())
                return Forbid();
        }

        var bookings = await _bookingService.GetActiveBookingsForStationAsync(stationId);
        return Ok(bookings);
    }

    /// <summary>
    /// Create a new booking (EVOwner only)
    /// </summary>
    /// <param name="request">Booking creation request</param>
    /// <returns>Created booking details</returns>
    [HttpPost]
    [Authorize(Policy = "EVOwnerOnly")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var booking = await _bookingService.CreateBookingAsync(GetCurrentUserId(), request);
        if (booking == null)
            return BadRequest(new { Message = "Failed to create booking. Station may be unavailable, slots full, or there is a booking conflict." });

        return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
    }

    /// <summary>
    /// Update a booking (EVOwner only, at least 12h before booking time)
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Booking update request</param>
    /// <returns>Updated booking details</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "EVOwnerOnly")]
    public async Task<IActionResult> UpdateBooking(string id, [FromBody] UpdateBookingRequest request)
    {
        // Check if booking can be modified
        var canModify = await _bookingService.CanModifyBookingAsync(id);
        if (!canModify)
            return BadRequest(new { Message = "Booking cannot be modified. It must be at least 12 hours before the booking time." });

        var booking = await _bookingService.UpdateBookingAsync(id, GetCurrentUserId(), request);
        if (booking == null)
            return NotFound(new { Message = "Booking not found, unauthorized, or there is a booking conflict." });

        return Ok(booking);
    }

    /// <summary>
    /// Cancel a booking 
    /// - EVOwner: at least 12h before booking time
    /// - Backoffice/Operator: anytime
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Cancellation request</param>
    /// <returns>Cancelled booking details</returns>
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelBooking(string id, [FromBody] CancelBookingRequest request)
    {
        var booking = await _bookingService.CancelBookingAsync(id, GetCurrentUserId(), request);
        if (booking == null)
            return NotFound(new { Message = "Booking not found, unauthorized, or cannot be cancelled." });

        return Ok(booking);
    }

    /// <summary>
    /// Approve a booking (Backoffice or station operator)
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Approved booking details</returns>
    [HttpPatch("{id}/approve")]
    [Authorize(Policy = "BackofficeOrOperator")]
    public async Task<IActionResult> ApproveBooking(string id)
    {
        var booking = await _bookingService.ApproveBookingAsync(id, GetCurrentUserId());
        if (booking == null)
            return NotFound(new { Message = "Booking not found, unauthorized, or cannot be approved." });

        return Ok(booking);
    }

    /// <summary>
    /// Complete a booking (Backoffice or station operator)
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Completion request</param>
    /// <returns>Completed booking details</returns>
    [HttpPatch("{id}/complete")]
    [Authorize(Policy = "BackofficeOrOperator")]
    public async Task<IActionResult> CompleteBooking(string id, [FromBody] CompleteBookingRequest request)
    {
        var booking = await _bookingService.CompleteBookingAsync(id, GetCurrentUserId(), request);
        if (booking == null)
            return NotFound(new { Message = "Booking not found, unauthorized, or cannot be completed." });

        return Ok(booking);
    }

    /// <summary>
    /// Generate QR code for a booking (EVOwner only, for approved bookings)
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Booking QR code</returns>
    [HttpGet("{id}/qr")]
    [Authorize(Policy = "EVOwnerOnly")]
    public async Task<IActionResult> GetBookingQR(string id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
            return NotFound(new { Message = "Booking not found" });

        // Only the booking owner can get their QR code
        if (booking.EVOwnerId != GetCurrentUserId())
            return Forbid();

        var qrResponse = await _bookingService.GenerateQRCodeAsync(id);
        if (qrResponse == null)
            return BadRequest(new { Message = "QR code could not be generated. Booking must be approved." });

        return Ok(qrResponse);
    }

    /// <summary>
    /// Validate a booking QR code (Backoffice or station operator)
    /// </summary>
    /// <param name="request">QR validation request</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate-qr")]
    [Authorize(Policy = "BackofficeOrOperator")]
    public async Task<IActionResult> ValidateQR([FromBody] QRValidationRequest request)
    {
        var validationResult = await _bookingService.ValidateQRCodeAsync(request);
        return Ok(validationResult);
    }

    /// <summary>
    /// Get booking counts for the current user (EVOwner only)
    /// </summary>
    /// <returns>Booking count statistics</returns>
    [HttpGet("my-counts")]
    [Authorize(Policy = "EVOwnerOnly")]
    public async Task<IActionResult> GetMyBookingCounts()
    {
        var counts = await _bookingService.GetBookingCountsForEVOwnerAsync(GetCurrentUserId());
        return Ok(counts);
    }

    /// <summary>
    /// Get booking counts for a specific station (Backoffice or station operator)
    /// </summary>
    /// <param name="stationId">Station ID</param>
    /// <returns>Booking count statistics</returns>
    [HttpGet("station/{stationId}/counts")]
    [Authorize(Policy = "BackofficeOrOperator")]
    public async Task<IActionResult> GetStationBookingCounts(string stationId)
    {
        // If operator, check if they operate this station
        if (HasRole("operator"))
        {
            var station = await _stationService.GetStationByIdAsync(stationId);
            if (station == null || station.OperatorId != GetCurrentUserId())
                return Forbid();
        }

        var counts = await _bookingService.GetBookingCountsForStationAsync(stationId);
        return Ok(counts);
    }
}