/*
 * File Name: JwtService.cs
 * Description: JWT token generation and password hashing implementation.
 */

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using EadChargingBookingBackend.Configuration;
using EadChargingBookingBackend.Models;
using EadChargingBookingBackend.DTOs;

namespace EadChargingBookingBackend.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;

    public JwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id!),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("userId", user.Id!),
            new("username", user.Username),
            new("role", user.Role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public AuthResponse CreateAuthResponse(User user, string token)
    {
        var User = new UserResponse(
            Id: user.Id!,
            Username: user.Username,
            Email: user.Email,
            Role: user.Role,
            NIC: user.NIC,
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt
        );

        return new AuthResponse(
            Token: token,
            Username: user.Username,
            Email: user.Email,
            Role: user.Role,
            ExpiresAt: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            User: User
        );
    }

    public string HashPassword(string password)
    {
        // Using BCrypt would be better in production, but for simplicity, using PBKDF2
        using var pbkdf2 = new Rfc2898DeriveBytes(password, GetSalt(), 10000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        var salt = pbkdf2.Salt;

        // Combine salt and hash
        var hashBytes = new byte[salt.Length + hash.Length];
        Array.Copy(salt, 0, hashBytes, 0, salt.Length);
        Array.Copy(hash, 0, hashBytes, salt.Length, hash.Length);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(hashedPassword);
            var salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            for (int i = 0; i < 32; i++)
            {
                if (hashBytes[i + 16] != hash[i])
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] GetSalt()
    {
        var salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }
}
