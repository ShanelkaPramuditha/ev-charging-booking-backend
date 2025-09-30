using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EadChargingBookingBackend.Configuration;

namespace EadChargingBookingBackend.Middleware;

public static class AuthenticationMiddleware
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Get JWT settings from centralized configuration manager
        var jwtSettings = EadChargingBookingBackend.Configuration.ConfigurationManager.GetJwtSettings(configuration);
        var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("OfficeUserOnly", policy =>
                policy.RequireRole("officeUser"));

            options.AddPolicy("OperatorOnly", policy =>
                policy.RequireRole("operator"));

            options.AddPolicy("AnyRole", policy =>
                policy.RequireRole("officeUser", "operator"));

            options.AddPolicy("PublicAccess", policy =>
                policy.RequireAssertion(_ => true)); // Allow all authenticated users
        });

        return services;
    }
}