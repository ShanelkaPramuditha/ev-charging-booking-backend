using System.Text.Json;
using EadChargingBookingBackend.Extensions;
using EadChargingBookingBackend.Middleware;
using AppConfig = EadChargingBookingBackend.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file using centralized configuration
AppConfig.ConfigurationManager.LoadEnvironmentVariables();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerConfiguration();

// Add application services with configuration
builder.Services.AddApplicationServices(builder.Configuration);

// Add JWT authentication from Middleware
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();

// Add CORS for frontend integration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Better JSON serialization for Controllers
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

// Configure JSON options for MVC
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.WriteIndented = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EAD Charging Booking API v1");
    });
}
else
{
    // Use global exception handling in production
    app.UseGlobalExceptionHandling();
}

app.UseHttpsRedirection();

// Custom middleware
app.UseRequestLogging(); // Log all requests

// Use CORS
app.UseCors();

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

app.Run();
