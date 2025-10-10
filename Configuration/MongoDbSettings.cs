/*
 * File Name: MongoDbSettings.cs
 * Description: MongoDB connection and database configuration settings.
 */

namespace EadChargingBookingBackend.Configuration;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;

    public void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException("MongoDB ConnectionString is required");

        if (string.IsNullOrWhiteSpace(DatabaseName))
            throw new InvalidOperationException("MongoDB DatabaseName is required");
    }
}