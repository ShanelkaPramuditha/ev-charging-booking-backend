/*
 * File Name: MongoUserRepository.cs
 * Description: MongoDB implementation of user repository.
 */

using MongoDB.Driver;
using EadChargingBookingBackend.Models;

namespace EadChargingBookingBackend.Repositories;

public class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public MongoUserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");

        // Create indexes for better performance
        CreateIndexes();
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _users.Find(_ => true).ToListAsync();
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByNICAsync(string nic)
    {
        // Only search for non-empty NIC values
        if (string.IsNullOrEmpty(nic))
            return null;

        return await _users.Find(u => u.NIC == nic && u.Role == "evOwner").FirstOrDefaultAsync();
    }

    public async Task<User> CreateAsync(string username, string email, string passwordHash, string role, string? nic = null)
    {
        // Set NIC only for EVOwner role, empty string for others
        var finalNic = role == "evOwner" ? (nic ?? throw new ArgumentException("NIC is required for EVOwner")) : "";
        var user = new User(username, email, passwordHash, role, finalNic);
        await _users.InsertOneAsync(user);
        return user;
    }

    public async Task<bool> UpdateAsync(string id, string username, string email, string role, string? nic, bool isActive)
    {
        var finalNic = role == "evOwner" ? (nic ?? "") : "";
        var update = Builders<User>.Update
            .Set(u => u.Username, username)
            .Set(u => u.Email, email)
            .Set(u => u.Role, role)
            .Set(u => u.NIC, finalNic)
            .Set(u => u.IsActive, isActive)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        var result = await _users.UpdateOneAsync(u => u.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _users.DeleteOneAsync(u => u.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        var count = await _users.CountDocumentsAsync(u => u.Username == username);
        return count > 0;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var count = await _users.CountDocumentsAsync(u => u.Email == email);
        return count > 0;
    }

    public async Task<bool> NICExistsAsync(string nic)
    {
        // Only check for non-empty NIC values
        if (string.IsNullOrEmpty(nic))
            return false;

        var count = await _users.CountDocumentsAsync(u => u.NIC == nic && u.Role == "evOwner");
        return count > 0;
    }

    private void CreateIndexes()
    {
        try
        {
            // First, try to drop the old unique NIC index if it exists
            try
            {
                _users.Indexes.DropOne("nic_1");
                Console.WriteLine("Dropped old unique NIC index");
            }
            catch (Exception)
            {
                // Index might not exist, ignore
            }

            // Create unique index for username
            var usernameIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Username);
            var usernameIndexOptions = new CreateIndexOptions { Unique = true };
            var usernameIndexModel = new CreateIndexModel<User>(usernameIndexKeys, usernameIndexOptions);

            // Create unique index for email
            var emailIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var emailIndexOptions = new CreateIndexOptions { Unique = true };
            var emailIndexModel = new CreateIndexModel<User>(emailIndexKeys, emailIndexOptions);

            // Create index for role
            var roleIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Role);
            var roleIndexModel = new CreateIndexModel<User>(roleIndexKeys);

            // Create regular index for NIC (no unique constraint to allow multiple empty strings)
            var nicIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.NIC);
            var nicIndexModel = new CreateIndexModel<User>(nicIndexKeys);

            _users.Indexes.CreateMany(new[] { usernameIndexModel, emailIndexModel, roleIndexModel, nicIndexModel });
        }
        catch (Exception ex)
        {
            // Log the error but don't throw
            Console.WriteLine($"Index creation error: {ex.Message}");
        }
    }
}
