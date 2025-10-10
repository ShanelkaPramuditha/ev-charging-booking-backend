using EadChargingBookingBackend.Models;
using EadChargingBookingBackend.DTOs;
using EadChargingBookingBackend.Repositories;

namespace EadChargingBookingBackend.Services;

public class ChargingStationService : IChargingStationService
{
    private readonly IChargingStationRepository _stationRepository;
    private readonly IUserRepository _userRepository;

    public ChargingStationService(IChargingStationRepository stationRepository, IUserRepository userRepository)
    {
        _stationRepository = stationRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<StationResponse>> GetAllStationsAsync()
    {
        var stations = await _stationRepository.GetAllAsync();
        return await MapToResponsesAsync(stations);
    }

    public async Task<IEnumerable<StationResponse>> GetActiveStationsAsync()
    {
        var stations = await _stationRepository.GetActiveAsync();
        return await MapToResponsesAsync(stations);
    }

    public async Task<StationResponse?> GetStationByIdAsync(string id)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null)
            return null;

        return await MapToResponseAsync(station);
    }

    public async Task<StationResponse?> CreateStationAsync(CreateStationRequest request)
    {
        // Check for duplicate name
        if (await _stationRepository.ExistsByNameAsync(request.Name))
            return null;

        // Convert schedule from DTO to model if provided
        List<ScheduleItem>? schedule = null;
        if (request.Schedule != null && request.Schedule.Any())
        {
            schedule = request.Schedule.Select(s => s.ToModel()).ToList();
        }

        var station = await _stationRepository.CreateAsync(
            request.Name,
            request.Location.ToModel(),
            request.Type,
            request.TotalSlots,
            request.OperatorId,
            request.Address,
            request.ContactPhone,
            schedule
        );

        return await MapToResponseAsync(station);
    }

    public async Task<StationResponse?> UpdateStationAsync(string id, UpdateStationRequest request)
    {
        var existingStation = await _stationRepository.GetByIdAsync(id);
        if (existingStation == null)
            return null;

        // Check for duplicate name if name is changing
        if (request.Name != existingStation.Name && await _stationRepository.ExistsByNameAsync(request.Name))
            return null;

        var updatedStation = existingStation.WithUpdatedInfo(
            request.Name,
            request.Location.ToModel(),
            request.Type,
            request.TotalSlots,
            request.Address,
            request.ContactPhone
        );

        if (await _stationRepository.UpdateAsync(updatedStation))
            return await MapToResponseAsync(updatedStation);

        return null;
    }

    public async Task<bool> DeleteStationAsync(string id)
    {
        // Check if station exists
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null)
            return false;

        // Check if there are active bookings
        if (await _stationRepository.HasActiveBookingsAsync(id))
            return false;

        return await _stationRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<StationResponse>> GetStationsByOperatorIdAsync(string operatorId)
    {
        var stations = await _stationRepository.GetByOperatorIdAsync(operatorId);
        return await MapToResponsesAsync(stations);
    }

    public async Task<IEnumerable<NearbyStationResponse>> GetNearbyStationsAsync(
        double latitude, double longitude, double maxDistanceKm, int limit = 10)
    {
        var stations = await _stationRepository.GetNearbyAsync(latitude, longitude, maxDistanceKm, limit);

        // Calculate distance and map to NearbyStationResponse
        var responses = new List<NearbyStationResponse>();
        foreach (var station in stations)
        {
            var distance = CalculateDistance(
                latitude, longitude,
                station.Location.Latitude, station.Location.Longitude
            );

            responses.Add(new NearbyStationResponse(
                station.Id!,
                station.Name,
                GeoLocationDto.FromModel(station.Location),
                station.Type,
                station.AvailableSlots,
                distance,
                station.Address
            ));
        }

        // Sort by distance
        return responses.OrderBy(s => s.DistanceKm);
    }

    public async Task<StationResponse?> UpdateAvailableSlotsAsync(string id, UpdateStationSlotsRequest request)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null)
            return null;

        // Validate slots are within bounds
        if (request.AvailableSlots < 0 || request.AvailableSlots > station.TotalSlots)
            return null;

        var updatedStation = station.WithAvailableSlots(request.AvailableSlots);

        if (await _stationRepository.UpdateAsync(updatedStation))
            return await MapToResponseAsync(updatedStation);

        return null;
    }

    public async Task<StationResponse?> UpdateScheduleAsync(string id, UpdateStationScheduleRequest request)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null)
            return null;

        // Validate schedule (ensure all days are covered, times are valid)
        var schedule = request.Schedule.Select(s => s.ToModel()).ToList();

        // Validate we have all 7 days of the week
        var daysCovered = schedule.Select(s => s.DayOfWeek).Distinct().Count();
        if (daysCovered != 7)
            return null;

        var updatedStation = station.WithSchedule(schedule);

        if (await _stationRepository.UpdateAsync(updatedStation))
            return await MapToResponseAsync(updatedStation);

        return null;
    }

    public async Task<StationResponse?> AssignOperatorAsync(string id, AssignOperatorRequest request)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null)
            return null;

        // Verify operator exists and has the operator role
        var operator_ = await _userRepository.GetByIdAsync(request.OperatorId);
        if (operator_ == null || operator_.Role != "operator")
            return null;

        var updatedStation = station.WithOperator(request.OperatorId);

        if (await _stationRepository.UpdateAsync(updatedStation))
            return await MapToResponseAsync(updatedStation);

        return null;
    }

    public async Task<StationResponse?> SetStationStatusAsync(string id, UpdateStationStatusRequest request)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null)
            return null;

        // If deactivating, ensure no active bookings
        if (!request.IsActive && station.IsActive)
        {
            if (await _stationRepository.HasActiveBookingsAsync(id))
                return null;
        }

        var updatedStation = station.WithStatus(request.IsActive);

        if (await _stationRepository.UpdateAsync(updatedStation))
            return await MapToResponseAsync(updatedStation);

        return null;
    }

    public async Task<bool> CanDeactivateStationAsync(string id)
    {
        // Station must exist
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null)
            return false;

        // Check if there are active bookings
        return !await _stationRepository.HasActiveBookingsAsync(id);
    }

    // Helper methods to map ChargingStation model to DTOs
    private async Task<StationResponse> MapToResponseAsync(ChargingStation station)
    {
        OperatorDetailsDto? operatorDetails = null;
        if (!string.IsNullOrEmpty(station.OperatorId))
        {
            var operator_ = await _userRepository.GetByIdAsync(station.OperatorId);
            if (operator_ != null)
            {
                operatorDetails = new OperatorDetailsDto(
                    operator_.Id!,
                    operator_.Username,
                    operator_.Email,
                    operator_.IsActive
                );
            }
        }

        return new StationResponse(
            station.Id!,
            station.Name,
            GeoLocationDto.FromModel(station.Location),
            station.Type,
            station.TotalSlots,
            station.AvailableSlots,
            station.OperatorId,
            operatorDetails,
            station.Schedule.Select(s => ScheduleItemDto.FromModel(s)).ToList(),
            station.IsActive,
            station.Address,
            station.ContactPhone,
            station.CreatedAt,
            station.UpdatedAt
        );
    }

    private async Task<IEnumerable<StationResponse>> MapToResponsesAsync(IEnumerable<ChargingStation> stations)
    {
        var responses = new List<StationResponse>();
        foreach (var station in stations)
        {
            responses.Add(await MapToResponseAsync(station));
        }
        return responses;
    }

    // Calculate distance between two points on Earth in kilometers using Haversine formula
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 6371; // Earth radius in kilometers

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}