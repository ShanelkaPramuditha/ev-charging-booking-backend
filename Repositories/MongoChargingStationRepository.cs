using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using Microsoft.Extensions.Options;
using EadChargingBookingBackend.Models;
using EadChargingBookingBackend.Configuration;

namespace EadChargingBookingBackend.Repositories;

public class MongoChargingStationRepository : IChargingStationRepository
{
    private readonly IMongoCollection<ChargingStation> _stations;
    private readonly IMongoCollection<Booking> _bookings;

    public MongoChargingStationRepository(IOptions<MongoDbSettings> mongoDbSettings)
    {
        var client = new MongoClient(mongoDbSettings.Value.ConnectionString);
        var database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
        _stations = database.GetCollection<ChargingStation>("chargingStations");
        _bookings = database.GetCollection<Booking>("bookings");

        // Create indexes for better performance
        CreateIndexes();
    }

    public async Task<IEnumerable<ChargingStation>> GetAllAsync()
    {
        return await _stations.Find(_ => true).ToListAsync();
    }

    public async Task<IEnumerable<ChargingStation>> GetActiveAsync()
    {
        return await _stations.Find(s => s.IsActive).ToListAsync();
    }

    public async Task<ChargingStation?> GetByIdAsync(string id)
    {
        return await _stations.Find(s => s.Id == id).FirstOrDefaultAsync();
    }

    public async Task<ChargingStation> CreateAsync(string name, GeoLocation location, string type, int totalSlots,
        string? operatorId, string address, string contactPhone, List<ScheduleItem>? schedule = null)
    {
        var station = new ChargingStation(name, location, type, totalSlots, operatorId, address, contactPhone);

        if (schedule != null && schedule.Count > 0)
        {
            station = station.WithSchedule(schedule);
        }
        else
        {
            // Create default schedule (open 24/7)
            var defaultSchedule = new List<ScheduleItem>();
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                defaultSchedule.Add(new ScheduleItem(day, TimeSpan.Zero, TimeSpan.FromDays(1).Subtract(TimeSpan.FromSeconds(1))));
            }
            station = station.WithSchedule(defaultSchedule);
        }

        await _stations.InsertOneAsync(station);
        return station;
    }

    public async Task<bool> UpdateAsync(ChargingStation station)
    {
        var result = await _stations.ReplaceOneAsync(s => s.Id == station.Id, station);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _stations.DeleteOneAsync(s => s.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<IEnumerable<ChargingStation>> GetByOperatorIdAsync(string operatorId)
    {
        return await _stations.Find(s => s.OperatorId == operatorId).ToListAsync();
    }

    public async Task<IEnumerable<ChargingStation>> GetNearbyAsync(double latitude, double longitude, double maxDistanceKm, int limit = 10)
    {
        // Using MongoDB's geospatial queries with spherical distance calculation
        // Calculate stations within maxDistanceKm of the given coordinates

        // We need to manually filter stations by calculating distance since Location isn't stored as GeoJson
        var stations = await _stations
            .Find(Builders<ChargingStation>.Filter.Eq(s => s.IsActive, true))
            .ToListAsync();

        // Calculate distance for each station and filter
        var nearbyStations = stations
            .Select(s => new
            {
                Station = s,
                Distance = CalculateDistance(latitude, longitude, s.Location.Latitude, s.Location.Longitude)
            })
            .Where(item => item.Distance <= maxDistanceKm)
            .OrderBy(item => item.Distance)
            .Take(limit)
            .Select(item => item.Station)
            .ToList();

        return nearbyStations;
    }

    // Haversine formula to calculate great-circle distance between two points in kilometers
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0; // Earth's radius in kilometers

        // Convert degrees to radians
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        // Haversine formula
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    public async Task<bool> UpdateAvailableSlotsAsync(string id, int availableSlots)
    {
        var update = Builders<ChargingStation>.Update
            .Set(s => s.AvailableSlots, availableSlots)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var result = await _stations.UpdateOneAsync(s => s.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateScheduleAsync(string id, List<ScheduleItem> schedule)
    {
        var update = Builders<ChargingStation>.Update
            .Set(s => s.Schedule, schedule)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var result = await _stations.UpdateOneAsync(s => s.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> AssignOperatorAsync(string id, string operatorId)
    {
        var update = Builders<ChargingStation>.Update
            .Set(s => s.OperatorId, operatorId)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var result = await _stations.UpdateOneAsync(s => s.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> SetActiveStatusAsync(string id, bool isActive)
    {
        var update = Builders<ChargingStation>.Update
            .Set(s => s.IsActive, isActive)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var result = await _stations.UpdateOneAsync(s => s.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> HasActiveBookingsAsync(string stationId)
    {
        var now = DateTime.UtcNow;

        // Get all approved bookings for this station
        // We filter by date first to reduce the data set
        var filter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
            Builders<Booking>.Filter.Eq(b => b.Status, BookingStatus.Approved),
            Builders<Booking>.Filter.Gte(b => b.BookingDate, now.Date.AddDays(-1)) // Get bookings from yesterday onwards
        );

        var bookings = await _bookings.Find(filter).ToListAsync();

        // Check in memory if any booking end time is in the future
        return bookings.Any(b => b.BookingDate.Add(b.TimeSlot.EndTime) > now);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        var count = await _stations.CountDocumentsAsync(s => s.Name == name);
        return count > 0;
    }

    private void CreateIndexes()
    {
        try
        {
            // Create 2dsphere index for geospatial queries
            var geoIndexKeys = Builders<ChargingStation>.IndexKeys
                .Geo2DSphere(s => s.Location);
            var geoIndexModel = new CreateIndexModel<ChargingStation>(geoIndexKeys);

            // Create index for operator ID
            var operatorIndexKeys = Builders<ChargingStation>.IndexKeys
                .Ascending(s => s.OperatorId);
            var operatorIndexModel = new CreateIndexModel<ChargingStation>(operatorIndexKeys);

            // Create index for name (with uniqueness constraint)
            var nameIndexKeys = Builders<ChargingStation>.IndexKeys
                .Ascending(s => s.Name);
            var nameIndexOptions = new CreateIndexOptions { Unique = true };
            var nameIndexModel = new CreateIndexModel<ChargingStation>(nameIndexKeys, nameIndexOptions);

            // Create index for IsActive field
            var activeIndexKeys = Builders<ChargingStation>.IndexKeys
                .Ascending(s => s.IsActive);
            var activeIndexModel = new CreateIndexModel<ChargingStation>(activeIndexKeys);

            _stations.Indexes.CreateMany(new[] {
                geoIndexModel, operatorIndexModel, nameIndexModel, activeIndexModel
            });
        }
        catch (Exception ex)
        {
            // Log the error but don't throw
            Console.WriteLine($"Index creation error: {ex.Message}");
        }
    }
}