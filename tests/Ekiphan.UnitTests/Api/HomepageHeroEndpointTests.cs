using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ekiphan.UnitTests.Api;

public sealed class HomepageHeroEndpointTests
{
    [Fact]
    public async Task AdminEndpointFailsClosedWithoutJwt()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/admin/content/homepage-heroes");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task PublicEndpointRejectsUnsupportedLanguageBeforeQuery()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/home/de/heroes");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting(
                "ConnectionStrings:EkiphanDatabase",
                "Server=(localdb)\\mssqllocaldb;Database=HeroEndpointTests;" +
                "Trusted_Connection=True"));
}
