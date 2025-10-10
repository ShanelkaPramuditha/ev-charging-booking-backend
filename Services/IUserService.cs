/*
 * File Name: IUserService.cs
 * Description: Interface for user service operations.
 */

using EadChargingBookingBackend.DTOs;

namespace EadChargingBookingBackend.Services;

public interface IUserService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> NICLoginAsync(NICLoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<IEnumerable<UserResponse>> GetAllUsersAsync();
    Task<UserResponse?> GetUserByIdAsync(string id);
    Task<bool> DeleteUserAsync(string id);

    // New methods for backoffice user management
    Task<UserResponse?> UpdateUserAsync(string id, UpdateUserRequest request);
    Task<UserResponse?> UpdateUserStatusAsync(string id, bool isActive);
    Task<IEnumerable<UserResponse>> GetUsersByRoleAsync(string role);
    Task<IEnumerable<UserResponse>> GetFilteredUsersAsync(string? role = null, string? search = null, bool? isActive = null);
    Task<UserResponse?> CreateUserAsync(CreateUserRequest request);
}