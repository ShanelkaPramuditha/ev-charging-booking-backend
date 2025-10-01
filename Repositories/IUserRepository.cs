using EadChargingBookingBackend.Models;

namespace EadChargingBookingBackend.Repositories;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByNICAsync(string nic);
    Task<User> CreateAsync(string username, string email, string passwordHash, string role, string? nic = null);
    Task<bool> UpdateAsync(string id, string username, string email, string role, string? nic, bool isActive);
    Task<bool> DeleteAsync(string id);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> NICExistsAsync(string nic);
}