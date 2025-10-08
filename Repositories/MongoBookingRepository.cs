using MongoDB.Driver;
using Microsoft.Extensions.Options;
using EadChargingBookingBackend.Models;
using EadChargingBookingBackend.Configuration;

namespace EadChargingBookingBackend.Repositories;

public class MongoBookingRepository : IBookingRepository
{
    private readonly IMongoCollection<Booking> _bookings;

    public MongoBookingRepository(IOptions<MongoDbSettings> mongoDbSettings)
    {
        var client = new MongoClient(mongoDbSettings.Value.ConnectionString);
        var database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
        _bookings = database.GetCollection<Booking>("bookings");

        // Create indexes for better performance
        CreateIndexes();
    }

    public async Task<IEnumerable<Booking>> GetAllAsync()
    {
        return await _bookings.Find(_ => true).ToListAsync();
    }

    public async Task<Booking?> GetByIdAsync(string id)
    {
        return await _bookings.Find(b => b.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Booking> CreateAsync(string evOwnerId, string stationId, DateTime bookingDate,
        TimeSlot timeSlot, string vehicleInfo = "", string notes = "")
    {
        var booking = new Booking(evOwnerId, stationId, bookingDate, timeSlot, vehicleInfo, notes);
        await _bookings.InsertOneAsync(booking);
        return booking;
    }

    public async Task<bool> UpdateAsync(Booking booking)
    {
        var result = await _bookings.ReplaceOneAsync(b => b.Id == booking.Id, booking);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _bookings.DeleteOneAsync(b => b.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<IEnumerable<Booking>> GetByEVOwnerIdAsync(string evOwnerId)
    {
        return await _bookings
            .Find(b => b.EVOwnerId == evOwnerId)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetByStationIdAsync(string stationId)
    {
        return await _bookings
            .Find(b => b.StationId == stationId)
            .SortByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status)
    {
        return await _bookings
            .Find(b => b.Status == status)
            .SortByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _bookings
            .Find(b => b.BookingDate >= startDate && b.BookingDate <= endDate)
            .SortByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetActiveBookingsAsync()
    {
        var now = DateTime.UtcNow;
        return await _bookings
            .Find(b => b.Status == BookingStatus.Approved &&
                 b.BookingDate >= now.Date)
            .SortByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetUpcomingBookingsForEVOwnerAsync(string evOwnerId)
    {
        var now = DateTime.UtcNow;
        return await _bookings
            .Find(b => b.EVOwnerId == evOwnerId &&
                 (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved) &&
                 b.BookingDate >= now.Date)
            .SortBy(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingHistoryForEVOwnerAsync(string evOwnerId)
    {
        var now = DateTime.UtcNow;
        return await _bookings
            .Find(b => b.EVOwnerId == evOwnerId &&
                 (b.Status == BookingStatus.Completed || b.Status == BookingStatus.Cancelled ||
                  b.Status == BookingStatus.NoShow ||
                  (b.BookingDate < now.Date && b.Status == BookingStatus.Approved)))
            .SortByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetActiveBookingsForStationAsync(string stationId)
    {
        var now = DateTime.UtcNow;
        return await _bookings
            .Find(b => b.StationId == stationId &&
                 (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved) &&
                 b.BookingDate >= now.Date)
            .SortBy(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task<bool> HasOverlappingBookingAsync(string stationId, DateTime bookingDate, TimeSlot timeSlot, string? excludeBookingId = null)
    {
        var filter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
            Builders<Booking>.Filter.Eq(b => b.BookingDate, bookingDate.Date),
            Builders<Booking>.Filter.Or(
                Builders<Booking>.Filter.Eq(b => b.Status, BookingStatus.Pending),
                Builders<Booking>.Filter.Eq(b => b.Status, BookingStatus.Approved)
            )
        );

        if (!string.IsNullOrEmpty(excludeBookingId))
        {
            filter = Builders<Booking>.Filter.And(
                filter,
                Builders<Booking>.Filter.Ne(b => b.Id, excludeBookingId)
            );
        }

        var bookings = await _bookings.Find(filter).ToListAsync();

        // Check for overlapping time slots
        return bookings.Any(b => b.TimeSlot.OverlapsWith(timeSlot));
    }

    public async Task<bool> UpdateStatusAsync(string id, BookingStatus status, string? cancellationReason = null)
    {
        var update = Builders<Booking>.Update
            .Set(b => b.Status, status)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);

        if (status == BookingStatus.Cancelled && !string.IsNullOrEmpty(cancellationReason))
        {
            update = update.Set(b => b.CancellationReason, cancellationReason);
        }

        var result = await _bookings.UpdateOneAsync(b => b.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> SetQRCodeAsync(string id, string qrCode)
    {
        var update = Builders<Booking>.Update
            .Set(b => b.QRCode, qrCode)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);

        var result = await _bookings.UpdateOneAsync(b => b.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<Booking?> GetByQRCodeAsync(string qrCode)
    {
        return await _bookings.Find(b => b.QRCode == qrCode).FirstOrDefaultAsync();
    }

    public async Task<int> GetPendingCountForUserAsync(string userId)
    {
        return (int)await _bookings.CountDocumentsAsync(
            b => b.EVOwnerId == userId && b.Status == BookingStatus.Pending
        );
    }

    public async Task<int> GetApprovedCountForUserAsync(string userId)
    {
        var now = DateTime.UtcNow;
        return (int)await _bookings.CountDocumentsAsync(
            b => b.EVOwnerId == userId && b.Status == BookingStatus.Approved && b.BookingDate >= now.Date
        );
    }

    public async Task<BookingStatus> GetBookingStatusAsync(string id)
    {
        var booking = await GetByIdAsync(id);
        return booking?.Status ?? BookingStatus.Cancelled;
    }

    private void CreateIndexes()
    {
        try
        {
            // Create index for EVOwnerId
            var evOwnerIndexKeys = Builders<Booking>.IndexKeys
                .Ascending(b => b.EVOwnerId);
            var evOwnerIndexModel = new CreateIndexModel<Booking>(evOwnerIndexKeys);

            // Create index for StationId
            var stationIndexKeys = Builders<Booking>.IndexKeys
                .Ascending(b => b.StationId);
            var stationIndexModel = new CreateIndexModel<Booking>(stationIndexKeys);

            // Create index for BookingDate
            var dateIndexKeys = Builders<Booking>.IndexKeys
                .Ascending(b => b.BookingDate);
            var dateIndexModel = new CreateIndexModel<Booking>(dateIndexKeys);

            // Create index for Status
            var statusIndexKeys = Builders<Booking>.IndexKeys
                .Ascending(b => b.Status);
            var statusIndexModel = new CreateIndexModel<Booking>(statusIndexKeys);

            // Create unique index for QRCode (sparse to allow nulls/empty)
            var qrIndexKeys = Builders<Booking>.IndexKeys
                .Ascending(b => b.QRCode);
            var qrIndexOptions = new CreateIndexOptions { Unique = true, Sparse = true };
            var qrIndexModel = new CreateIndexModel<Booking>(qrIndexKeys, qrIndexOptions);

            // Create compound index for station+date+status (for overlapping checks)
            var compoundIndexKeys = Builders<Booking>.IndexKeys
                .Ascending(b => b.StationId)
                .Ascending(b => b.BookingDate)
                .Ascending(b => b.Status);
            var compoundIndexModel = new CreateIndexModel<Booking>(compoundIndexKeys);

            _bookings.Indexes.CreateMany(new[] {
                evOwnerIndexModel, stationIndexModel, dateIndexModel,
                statusIndexModel, qrIndexModel, compoundIndexModel
            });
        }
        catch (Exception ex)
        {
            // Log the error but don't throw
            Console.WriteLine($"Index creation error: {ex.Message}");
        }
    }
}