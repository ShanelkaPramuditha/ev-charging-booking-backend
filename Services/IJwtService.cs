using EadChargingBookingBackend.Models;
using EadChargingBookingBackend.DTOs;

namespace EadChargingBookingBackend.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    AuthResponse CreateAuthResponse(User user, string token);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}
