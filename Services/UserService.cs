/*
 * File Name: UserService.cs
 * Description: Business logic for user management and authentication.
 */

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

    public async Task<UserResponse?> UpdateUserAsync(string id, UpdateUserRequest request)
    {
        // Validate role
        if (!request.IsValidRole())
            return null;

        // Validate NIC requirement for EVOwner
        if (!request.IsValidNIC())
            return null;

        // Get existing user
        var existingUser = await _userRepository.GetByIdAsync(id);
        if (existingUser == null)
            return null;

        // Check if username is changed and already exists
        if (existingUser.Username != request.Username && await _userRepository.UsernameExistsAsync(request.Username))
            return null;

        // Check if email is changed and already exists
        if (existingUser.Email != request.Email && await _userRepository.EmailExistsAsync(request.Email))
            return null;

        // Check if NIC is changed and already exists (for EVOwner)
        var nic = request.NIC ?? "";
        if (existingUser.NIC != nic && request.Role == "evOwner" &&
            !string.IsNullOrWhiteSpace(nic) && await _userRepository.NICExistsAsync(nic))
            return null;

        // Determine isActive status (keep existing if not specified in request)
        var isActive = request.IsActive ?? existingUser.IsActive;

        // Update user
        var success = await _userRepository.UpdateAsync(
            id,
            request.Username,
            request.Email,
            request.Role,
            nic,
            isActive
        );

        if (!success)
            return null;

        // Return updated user
        var updatedUser = await _userRepository.GetByIdAsync(id);
        return updatedUser != null ? new UserResponse(
            updatedUser.Id!,
            updatedUser.Username,
            updatedUser.Email,
            updatedUser.Role,
            string.IsNullOrEmpty(updatedUser.NIC) ? null : updatedUser.NIC,
            updatedUser.IsActive,
            updatedUser.CreatedAt,
            updatedUser.UpdatedAt
        ) : null;
    }

    public async Task<UserResponse?> UpdateUserStatusAsync(string id, bool isActive)
    {
        // Get existing user
        var existingUser = await _userRepository.GetByIdAsync(id);
        if (existingUser == null)
            return null;

        // Update only the isActive status
        var success = await _userRepository.UpdateAsync(
            id,
            existingUser.Username,
            existingUser.Email,
            existingUser.Role,
            existingUser.NIC,
            isActive
        );

        if (!success)
            return null;

        // Return updated user
        var updatedUser = await _userRepository.GetByIdAsync(id);
        return updatedUser != null ? new UserResponse(
            updatedUser.Id!,
            updatedUser.Username,
            updatedUser.Email,
            updatedUser.Role,
            string.IsNullOrEmpty(updatedUser.NIC) ? null : updatedUser.NIC,
            updatedUser.IsActive,
            updatedUser.CreatedAt,
            updatedUser.UpdatedAt
        ) : null;
    }

    public async Task<IEnumerable<UserResponse>> GetUsersByRoleAsync(string role)
    {
        var users = await _userRepository.GetAllAsync();
        return users
            .Where(u => u.Role == role)
            .Select(u => new UserResponse(
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

    public async Task<IEnumerable<UserResponse>> GetFilteredUsersAsync(string? role = null, string? search = null, bool? isActive = null)
    {
        var users = await _userRepository.GetAllAsync();
        var filteredUsers = users.AsEnumerable();

        // Filter by role if provided
        if (!string.IsNullOrEmpty(role))
        {
            filteredUsers = filteredUsers.Where(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by active status if provided
        if (isActive.HasValue)
        {
            filteredUsers = filteredUsers.Where(u => u.IsActive == isActive.Value);
        }

        // Filter by search term if provided (searches in username, email, and NIC)
        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            filteredUsers = filteredUsers.Where(u =>
                u.Username.ToLower().Contains(searchLower) ||
                u.Email.ToLower().Contains(searchLower) ||
                (!string.IsNullOrEmpty(u.NIC) && u.NIC.ToLower().Contains(searchLower))
            );
        }

        return filteredUsers.Select(u => new UserResponse(
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

    public async Task<UserResponse?> CreateUserAsync(CreateUserRequest request)
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

        // Create user (users created by backoffice are active by default)
        var user = await _userRepository.CreateAsync(
            request.Username,
            request.Email,
            passwordHash,
            request.Role,
            request.NIC
        );

        // Return user response (without token)
        return new UserResponse(
            user.Id!,
            user.Username,
            user.Email,
            user.Role,
            string.IsNullOrEmpty(user.NIC) ? null : user.NIC,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt
        );
    }
}
