/*
 * File Name: IChargingStationService.cs
 * Description: Interface for charging station service operations.
 */

using EadChargingBookingBackend.Models;
using EadChargingBookingBackend.DTOs;

namespace EadChargingBookingBackend.Services;

public interface IChargingStationService
{
    // Basic CRUD operations
    Task<IEnumerable<StationResponse>> GetAllStationsAsync();
    Task<IEnumerable<StationResponse>> GetActiveStationsAsync();
    Task<StationResponse?> GetStationByIdAsync(string id);
    Task<StationResponse?> CreateStationAsync(CreateStationRequest request);
    Task<StationResponse?> UpdateStationAsync(string id, UpdateStationRequest request);
    Task<bool> DeleteStationAsync(string id);

    // Specialized operations
    Task<IEnumerable<StationResponse>> GetStationsByOperatorIdAsync(string operatorId);
    Task<IEnumerable<NearbyStationResponse>> GetNearbyStationsAsync(double latitude, double longitude, double maxDistanceKm, int limit = 10);
    Task<StationResponse?> UpdateAvailableSlotsAsync(string id, UpdateStationSlotsRequest request);
    Task<StationResponse?> UpdateScheduleAsync(string id, UpdateStationScheduleRequest request);
    Task<StationResponse?> AssignOperatorAsync(string id, AssignOperatorRequest request);
    Task<StationResponse?> SetStationStatusAsync(string id, UpdateStationStatusRequest request);
    Task<bool> CanDeactivateStationAsync(string id);
}