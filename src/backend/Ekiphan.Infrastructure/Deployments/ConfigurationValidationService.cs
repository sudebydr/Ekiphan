namespace Ekiphan.Infrastructure.Deployments;

using System;
using Ekiphan.Application.Deployments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public class ConfigurationValidationService : IConfigurationValidationService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public ConfigurationValidationService(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public void ValidateStartupConfiguration()
    {
        var sqlConnection = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(sqlConnection))
        {
            throw new InvalidOperationException("CRITICAL: SQL Server connection string is missing.");
        }

        if (_environment.IsProduction())
        {
            var redisConnection = _configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(redisConnection))
            {
                throw new InvalidOperationException("CRITICAL: Redis connection string is missing in Production environment.");
            }

            var jwtKey = _configuration["JwtSettings:SigningKey"];
            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            {
                throw new InvalidOperationException("CRITICAL: JWT Signing Key is missing or too short in Production environment.");
            }

            var allowedOrigins = _configuration["Cors:AllowedOrigins"];
            if (allowedOrigins == "*")
            {
                throw new InvalidOperationException("CRITICAL: Wildcard CORS is not allowed in Production environment.");
            }
        }
    }
}
