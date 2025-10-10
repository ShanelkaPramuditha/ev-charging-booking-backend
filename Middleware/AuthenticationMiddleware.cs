/*
 * File Name: AuthenticationMiddleware.cs
 * Description: Middleware for JWT authentication configuration.
 */

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AppConfig = EadChargingBookingBackend.Configuration;

namespace EadChargingBookingBackend.Middleware;

public static class AuthenticationMiddleware
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Get JWT settings from centralized configuration manager
        var jwtSettings = AppConfig.ConfigurationManager.GetJwtSettings(configuration);
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
            options.AddPolicy("BackOfficeOnly", policy =>
                policy.RequireRole("backOffice"));

            options.AddPolicy("OperatorOnly", policy =>
                policy.RequireRole("operator"));

            options.AddPolicy("EVOwnerOnly", policy =>
                policy.RequireRole("evOwner"));

            options.AddPolicy("BackOfficeOrOperator", policy =>
                policy.RequireRole("backOffice", "operator"));

            options.AddPolicy("AnyRole", policy =>
                policy.RequireRole("backOffice", "operator", "evOwner"));

            options.AddPolicy("PublicAccess", policy =>
                policy.RequireAssertion(_ => true)); // Allow all authenticated users
        });

        return services;
    }
}