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
        if (!_environment.IsProduction())
        {
            return;
        }

        RequireValue(_configuration.GetConnectionString("EkiphanDatabase"), "ConnectionStrings:EkiphanDatabase");
        RequireValue(_configuration.GetConnectionString("Redis"), "ConnectionStrings:Redis");

        var jwtSigningKey = RequireValue(_configuration["Authentication:Jwt:SigningKey"], "Authentication:Jwt:SigningKey");
        if (jwtSigningKey.Length < 32)
        {
            throw new InvalidOperationException("Invalid required configuration: Authentication:Jwt:SigningKey (minimum 32 characters).");
        }

        RequireValue(_configuration["Authentication:Jwt:Issuer"], "Authentication:Jwt:Issuer");
        RequireValue(_configuration["Authentication:Jwt:Audience"], "Authentication:Jwt:Audience");

        var mediaProvider = RequireValue(_configuration["MediaStorage:Provider"], "MediaStorage:Provider");
        if (!string.Equals(mediaProvider, "S3", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid required configuration: MediaStorage:Provider must be 'S3' in Production.");
        }

        RequireValue(_configuration["MediaStorage:S3:Bucket"], "MediaStorage:S3:Bucket");
        RequireValue(_configuration["MediaStorage:S3:Region"], "MediaStorage:S3:Region");
        ValidatePublicMediaBaseUrl(RequireValue(_configuration["PublicMedia:BaseUrl"], "PublicMedia:BaseUrl"));
    }

    private static string RequireValue(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required configuration: {key}.");
        }

        return value;
    }

    private static void ValidatePublicMediaBaseUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException("Invalid required configuration: PublicMedia:BaseUrl must be an absolute HTTPS URL in Production.");
        }
    }
}
