using DotNetEnv;

namespace EadChargingBookingBackend.Configuration;

public static class ConfigurationManager
{
    // Load environment variables from .env files
    public static void LoadEnvironmentVariables()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var environment = Environment.GetEnvironmentVariable(EnvironmentVariables.ASPNETCORE_ENVIRONMENT) ?? "Development";

        // Load environment-specific .env file first (higher priority)
        var envSpecificFile = Path.Combine(currentDirectory, $".env.{environment.ToLower()}");
        if (File.Exists(envSpecificFile))
        {
            Console.WriteLine($"Loading environment-specific config: {envSpecificFile}");
            Env.Load(envSpecificFile);
        }

        // Load general .env file (lower priority, won't override existing variables)
        var envFile = Path.Combine(currentDirectory, ".env");
        if (File.Exists(envFile))
        {
            Console.WriteLine($"Loading general config: {envFile}");
            Env.Load(envFile, new LoadOptions(clobberExistingVars: false));
        }

        Console.WriteLine($"Current environment: {environment}");
    }

    // Configure all settings and validate
    public static void ConfigureAllSettings(IServiceCollection services, IConfiguration configuration)
    {
        // Configure MongoDB settings
        services.Configure<MongoDbSettings>(options =>
        {
            options.ConnectionString = GetEnvironmentVariable(EnvironmentVariables.MONGODB_CONNECTION_STRING)
                                     ?? configuration["MongoDbSettings:ConnectionString"]
                                     ?? throw new InvalidOperationException("MongoDB connection string not found");

            options.DatabaseName = GetEnvironmentVariable(EnvironmentVariables.MONGODB_DATABASE_NAME)
                                 ?? configuration["MongoDbSettings:DatabaseName"]
                                 ?? "EadChargingBookingDb";

            options.ValidateSettings();
        });

        // Configure JWT settings
        services.Configure<JwtSettings>(options =>
        {
            options.SecretKey = GetEnvironmentVariable(EnvironmentVariables.JWT_SECRET_KEY)
                              ?? configuration["JwtSettings:SecretKey"]
                              ?? throw new InvalidOperationException("JWT SecretKey not found");

            options.Issuer = GetEnvironmentVariable(EnvironmentVariables.JWT_ISSUER)
                           ?? configuration["JwtSettings:Issuer"]
                           ?? "EadChargingBookingAPI";

            options.Audience = GetEnvironmentVariable(EnvironmentVariables.JWT_AUDIENCE)
                             ?? configuration["JwtSettings:Audience"]
                             ?? "EadChargingBookingClient";

            options.ExpirationMinutes = int.TryParse(GetEnvironmentVariable(EnvironmentVariables.JWT_EXPIRATION_MINUTES), out var expiration)
                                      ? expiration
                                      : configuration.GetValue<int>("JwtSettings:ExpirationMinutes", 60);

            options.ValidateSettings();
        });
    }

    // Helper methods to get settings directly
    public static JwtSettings GetJwtSettings(IConfiguration configuration)
    {
        var secretKey = GetEnvironmentVariable(EnvironmentVariables.JWT_SECRET_KEY);
        if (string.IsNullOrEmpty(secretKey))
        {
            secretKey = configuration["JwtSettings:SecretKey"];
        }

        var issuer = GetEnvironmentVariable(EnvironmentVariables.JWT_ISSUER);
        if (string.IsNullOrEmpty(issuer))
        {
            issuer = configuration["JwtSettings:Issuer"] ?? "EadChargingBookingAPI";
        }

        var audience = GetEnvironmentVariable(EnvironmentVariables.JWT_AUDIENCE);
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
            ExpirationMinutes = int.TryParse(GetEnvironmentVariable(EnvironmentVariables.JWT_EXPIRATION_MINUTES), out var expiration)
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
            ConnectionString = GetEnvironmentVariable(EnvironmentVariables.MONGODB_CONNECTION_STRING)
                             ?? throw new InvalidOperationException("MongoDB connection string not found"),

            DatabaseName = GetEnvironmentVariable(EnvironmentVariables.MONGODB_DATABASE_NAME)
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

    // Environment variable keys
    public static class EnvironmentVariables
    {
        // General
        public const string ASPNETCORE_ENVIRONMENT = "ASPNETCORE_ENVIRONMENT";

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