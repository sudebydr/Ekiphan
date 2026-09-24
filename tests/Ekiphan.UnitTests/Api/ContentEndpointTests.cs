using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Ekiphan.UnitTests.Api;

public sealed class ContentEndpointTests
{
    [Fact]
    public async Task AdminEndpointsFailClosedWithoutJwtConfiguration()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/admin/content/pages");

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task PublicEndpointRejectsUnsupportedLanguageBeforeQuery()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/content/de/pages/about");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.UseSetting(
                    "ConnectionStrings:EkiphanDatabase",
                    "Server=(localdb)\\mssqllocaldb;" +
                    "Database=ContentEndpointTests;" +
                    "Trusted_Connection=True"));
}

