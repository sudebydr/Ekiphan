namespace Ekiphan.Infrastructure.Deployments;

using System;
using Ekiphan.Application.Deployments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public class ConfigurationValidationService : IConfigurationValidationService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public ConfigurationValidationService(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public void ValidateStartupConfiguration()
    {
        if (!_environment.IsProduction())
            return;

        RequireValue(
            _configuration.GetConnectionString("EkiphanDatabase"),
            "ConnectionStrings:EkiphanDatabase");

        var jwtSigningKey = RequireValue(
            _configuration["Authentication:Jwt:SigningKey"],
            "Authentication:Jwt:SigningKey");

        if (jwtSigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:SigningKey must be at least 32 characters.");
        }

        RequireValue(
            _configuration["Authentication:Jwt:Issuer"],
            "Authentication:Jwt:Issuer");

        RequireValue(
            _configuration["Authentication:Jwt:Audience"],
            "Authentication:Jwt:Audience");

        var mediaProvider = RequireValue(
            _configuration["MediaStorage:Provider"],
            "MediaStorage:Provider");

        if (string.Equals(mediaProvider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            RequireValue(
                _configuration["MediaStorage:LocalRoot"],
                "MediaStorage:LocalRoot");

            return;
        }

        if (string.Equals(mediaProvider, "S3", StringComparison.OrdinalIgnoreCase))
        {
            RequireValue(
                _configuration["MediaStorage:S3:Bucket"],
                "MediaStorage:S3:Bucket");

            RequireValue(
                _configuration["MediaStorage:S3:Region"],
                "MediaStorage:S3:Region");

            ValidatePublicMediaBaseUrl(
                RequireValue(
                    _configuration["PublicMedia:BaseUrl"],
                    "PublicMedia:BaseUrl"));

            return;
        }

        throw new InvalidOperationException(
            "MediaStorage:Provider must be 'Local' or 'S3'.");
    }

    private static string RequireValue(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Missing required configuration: {key}.");

        return value;
    }

    private static void ValidatePublicMediaBaseUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException(
                "PublicMedia:BaseUrl must be an absolute HTTPS URL.");
        }
    }
}
