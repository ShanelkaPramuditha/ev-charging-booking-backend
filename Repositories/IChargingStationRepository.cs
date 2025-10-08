using EadChargingBookingBackend.Models;

namespace EadChargingBookingBackend.Repositories;

public interface IChargingStationRepository
{
    // Basic CRUD operations
    Task<IEnumerable<ChargingStation>> GetAllAsync();
    Task<IEnumerable<ChargingStation>> GetActiveAsync();
    Task<ChargingStation?> GetByIdAsync(string id);
    Task<ChargingStation> CreateAsync(
        string name, GeoLocation location, string type, int totalSlots,
        string? operatorId, string address, string contactPhone, List<ScheduleItem>? schedule = null);
    Task<bool> UpdateAsync(ChargingStation station);
    Task<bool> DeleteAsync(string id);

    // Specialized operations
    Task<IEnumerable<ChargingStation>> GetByOperatorIdAsync(string operatorId);
    Task<IEnumerable<ChargingStation>> GetNearbyAsync(double latitude, double longitude, double maxDistanceKm, int limit = 10);
    Task<bool> UpdateAvailableSlotsAsync(string id, int availableSlots);
    Task<bool> UpdateScheduleAsync(string id, List<ScheduleItem> schedule);
    Task<bool> AssignOperatorAsync(string id, string operatorId);
    Task<bool> SetActiveStatusAsync(string id, bool isActive);
    Task<bool> HasActiveBookingsAsync(string stationId);
    Task<bool> ExistsByNameAsync(string name);
}