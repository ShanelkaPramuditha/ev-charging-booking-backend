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
}