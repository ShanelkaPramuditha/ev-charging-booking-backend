using EadChargingBookingBackend.DTOs;
using EadChargingBookingBackend.Repositories;

namespace EadChargingBookingBackend.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public UserService(IUserRepository userRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null || !user.IsActive)
            return null;

        if (!_jwtService.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        var token = _jwtService.GenerateToken(user);
        return _jwtService.CreateAuthResponse(user, token);
    }

    public async Task<AuthResponse?> NICLoginAsync(NICLoginRequest request)
    {
        var user = await _userRepository.GetByNICAsync(request.NIC);

        if (user == null || !user.IsActive || user.Role != "evOwner")
            return null;

        if (!_jwtService.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        var token = _jwtService.GenerateToken(user);
        return _jwtService.CreateAuthResponse(user, token);
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        // Validate role
        if (!request.IsValidRole())
            return null;

        // Validate NIC requirement for EVOwner
        if (!request.IsValidNIC())
            return null;

        // Check if username already exists
        if (await _userRepository.UsernameExistsAsync(request.Username))
            return null;

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(request.Email))
            return null;

        // Check if NIC already exists (for EVOwner)
        if (request.IsNICRequired() && !string.IsNullOrWhiteSpace(request.NIC) && await _userRepository.NICExistsAsync(request.NIC))
            return null;

        // Hash password
        var passwordHash = _jwtService.HashPassword(request.Password);

        // Create user
        var user = await _userRepository.CreateAsync(
            request.Username,
            request.Email,
            passwordHash,
            request.Role,
            request.NIC
        );

        // Generate token and return response
        var token = _jwtService.GenerateToken(user);
        return _jwtService.CreateAuthResponse(user, token);
    }

    public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(u => new UserResponse(
            u.Id!,
            u.Username,
            u.Email,
            u.Role,
            string.IsNullOrEmpty(u.NIC) ? null : u.NIC,
            u.IsActive,
            u.CreatedAt,
            u.UpdatedAt
        ));
    }

    public async Task<UserResponse?> GetUserByIdAsync(string id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user != null ? new UserResponse(
            user.Id!,
            user.Username,
            user.Email,
            user.Role,
            string.IsNullOrEmpty(user.NIC) ? null : user.NIC,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt
        ) : null;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        return await _userRepository.DeleteAsync(id);
    }
}
