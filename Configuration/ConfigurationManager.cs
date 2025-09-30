using DotNetEnv;

namespace EadChargingBookingBackend.Configuration;

public static class ConfigurationManager
{
    public static void LoadEnvironmentVariables()
    {
        // Load .env file if it exists
        var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envFile))
        {
            Env.Load(envFile);
        }
    }

    public static void ConfigureAllSettings(IServiceCollection services, IConfiguration configuration)
    {
        // Configure MongoDB settings
        services.Configure<MongoDbSettings>(options =>
        {
            options.ConnectionString = GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
                                     ?? configuration["MongoDbSettings:ConnectionString"]
                                     ?? throw new InvalidOperationException("MongoDB connection string not found");

            options.DatabaseName = GetEnvironmentVariable("MONGODB_DATABASE_NAME")
                                 ?? configuration["MongoDbSettings:DatabaseName"]
                                 ?? "EadChargingBookingDb";

            options.ValidateSettings();
        });

        // Configure JWT settings
        services.Configure<JwtSettings>(options =>
        {
            options.SecretKey = GetEnvironmentVariable("JWT_SECRET_KEY")
                              ?? configuration["JwtSettings:SecretKey"]
                              ?? throw new InvalidOperationException("JWT SecretKey not found");

            options.Issuer = GetEnvironmentVariable("JWT_ISSUER")
                           ?? configuration["JwtSettings:Issuer"]
                           ?? "EadChargingBookingAPI";

            options.Audience = GetEnvironmentVariable("JWT_AUDIENCE")
                             ?? configuration["JwtSettings:Audience"]
                             ?? "EadChargingBookingClient";

            options.ExpirationMinutes = int.TryParse(GetEnvironmentVariable("JWT_EXPIRATION_MINUTES"), out var expiration)
                                      ? expiration
                                      : configuration.GetValue<int>("JwtSettings:ExpirationMinutes", 60);

            options.ValidateSettings();
        });
    }

    public static JwtSettings GetJwtSettings(IConfiguration configuration)
    {
        var secretKey = GetEnvironmentVariable("JWT_SECRET_KEY");
        if (string.IsNullOrEmpty(secretKey))
        {
            secretKey = configuration["JwtSettings:SecretKey"];
        }

        var issuer = GetEnvironmentVariable("JWT_ISSUER");
        if (string.IsNullOrEmpty(issuer))
        {
            issuer = configuration["JwtSettings:Issuer"] ?? "EadChargingBookingAPI";
        }

        var audience = GetEnvironmentVariable("JWT_AUDIENCE");
        if (string.IsNullOrEmpty(audience))
        {
            audience = configuration["JwtSettings:Audience"] ?? "EadChargingBookingClient";
        }

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey not found in environment variables or configuration. " +
                "Please set JWT_SECRET_KEY environment variable or add JwtSettings:SecretKey to appsettings.json");
        }

        var jwtSettings = new JwtSettings
        {
            SecretKey = secretKey,
            Issuer = issuer,
            Audience = audience,
            ExpirationMinutes = int.TryParse(GetEnvironmentVariable("JWT_EXPIRATION_MINUTES"), out var expiration)
                              ? expiration
                              : configuration.GetValue<int>("JwtSettings:ExpirationMinutes", 60)
        };

        jwtSettings.ValidateSettings();
        return jwtSettings;
    }

    public static MongoDbSettings GetMongoDbSettings(IConfiguration configuration)
    {
        var mongoSettings = new MongoDbSettings
        {
            ConnectionString = GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
                             ?? configuration["MongoDbSettings:ConnectionString"]
                             ?? throw new InvalidOperationException("MongoDB connection string not found"),

            DatabaseName = GetEnvironmentVariable("MONGODB_DATABASE_NAME")
                         ?? configuration["MongoDbSettings:DatabaseName"]
                         ?? "EadChargingBookingDb"
        };

        mongoSettings.ValidateSettings();
        return mongoSettings;
    }

    public static string GetRequiredEnvironmentVariable(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Environment variable '{key}' is required but not set.");
        }
        return value;
    }

    public static string GetEnvironmentVariable(string key, string defaultValue = "")
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }

    // Centralized environment variable mapping
    public static class EnvironmentVariables
    {
        // MongoDB
        public const string MONGODB_CONNECTION_STRING = "MONGODB_CONNECTION_STRING";
        public const string MONGODB_DATABASE_NAME = "MONGODB_DATABASE_NAME";

        // JWT
        public const string JWT_SECRET_KEY = "JWT_SECRET_KEY";
        public const string JWT_ISSUER = "JWT_ISSUER";
        public const string JWT_AUDIENCE = "JWT_AUDIENCE";
        public const string JWT_EXPIRATION_MINUTES = "JWT_EXPIRATION_MINUTES";
    }
}