using EadChargingBookingBackend.Services;
using EadChargingBookingBackend.Repositories;
using AppConfig = EadChargingBookingBackend.Configuration;

namespace EadChargingBookingBackend.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure all settings using centralized configuration manager
        AppConfig.ConfigurationManager.ConfigureAllSettings(services, configuration);

        // Register repositories
        services.AddScoped<IUserRepository, MongoUserRepository>();
        services.AddScoped<IChargingStationRepository, MongoChargingStationRepository>();
        services.AddScoped<IBookingRepository, MongoBookingRepository>();

        // Register services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IChargingStationService, ChargingStationService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IQRCodeService, QRCodeService>();

        return services;
    }



    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "EAD Charging Booking API",
                Version = "v1",
                Description = "API for EAD Charging Booking Backend with JWT Authentication and Role-based Authorization"
            });

            // Add JWT Authentication to Swagger
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
            });

            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });


        });

        return services;
    }
}
