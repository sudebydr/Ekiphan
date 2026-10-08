using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ekiphan.UnitTests.Api;

public sealed class HttpHardeningTests
{
    [Fact]
    public async Task LivenessIsSafeJsonWithSecurityHeaders()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health/live");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "nosniff",
            response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(
            "DENY",
            response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal(
            "no-store",
            response.Headers.CacheControl?.ToString());
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        using var json = JsonDocument.Parse(body);
        Assert.Equal(
            "healthy",
            json.RootElement.GetProperty("status").GetString());
        Assert.Empty(
            json.RootElement.GetProperty("checks").EnumerateArray());
    }

    [Fact]
    public async Task ValidCorrelationIdIsPreserved()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        const string correlationId = "client-request_12345";
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        using var response = await client.GetAsync("/api/health");

        Assert.Equal(
            correlationId,
            response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task InvalidCorrelationIdIsReplaced()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "bad id");

        using var response = await client.GetAsync("/api/health");
        var returned = response.Headers
            .GetValues("X-Correlation-ID")
            .Single();

        Assert.NotEqual("bad id", returned);
        Assert.Matches("^[a-zA-Z0-9_-]{8,64}$", returned);
    }

    [Fact]
    public async Task RateLimitReturnsProblemDetailsAndRetryAfter()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        HttpResponseMessage? response = null;
        for (var index = 0; index < 6; index++)
        {
            response?.Dispose();
            response = await client.PostAsJsonAsync(
                "/api/quotes",
                new
                {
                    fullName = "",
                    companyName = "",
                    phone = "",
                    email = "",
                    country = "",
                    languageCode = "tr",
                    kvkkConsent = false,
                    commercialCommunicationConsent = false,
                    items = Array.Empty<object>(),
                });
        }

        using (response)
        {
            Assert.NotNull(response);
            Assert.Equal(
                HttpStatusCode.TooManyRequests,
                response.StatusCode);
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);
            Assert.True(response.Headers.RetryAfter is not null);
            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            Assert.Equal(
                StatusCodes.Status429TooManyRequests,
                json.RootElement.GetProperty("status").GetInt32());
        }
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.UseEnvironment("Testing")
                    .UseSetting("MediaStorage:Provider", "Local").UseSetting(
                    "ConnectionStrings:EkiphanDatabase",
                    "Server=(localdb)\\mssqllocaldb;" +
                    "Database=HttpHardeningTests;" +
                    "Trusted_Connection=True"));
}
