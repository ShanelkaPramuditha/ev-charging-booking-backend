using EadChargingBookingBackend.Models;

namespace EadChargingBookingBackend.Repositories;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(string username, string email, string passwordHash, string role);
    Task<bool> UpdateAsync(string id, string username, string email, string role, bool isActive);
    Task<bool> DeleteAsync(string id);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
}