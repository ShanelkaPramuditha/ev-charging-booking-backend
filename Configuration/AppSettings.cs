/*
 * File Name: AppSettings.cs
 * Description: Root application settings configuration container.
 */

namespace EadChargingBookingBackend.Configuration;

public class AppSettings
{
    public MongoDbSettings MongoDbSettings { get; set; } = new();
}




