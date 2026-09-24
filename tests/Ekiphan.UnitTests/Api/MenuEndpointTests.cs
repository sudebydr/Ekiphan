using System.Net;
using System.Net.Http.Json;
using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ekiphan.UnitTests.Api;

public sealed class MenuEndpointTests
{
    [Fact]
    public async Task AdminEndpointsFailClosedWithoutJwtConfiguration()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/admin/content/menu-items");

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Theory]
    [InlineData("/api/navigation/de/Header")]
    [InlineData("/api/navigation/tr/12")]
    [InlineData("/api/navigation/tr/Sidebar")]
    public async Task PublicEndpointRejectsUnsupportedRouteBeforeQuery(
        string path)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("tr", MenuLocation.Header)]
    [InlineData("en", MenuLocation.Footer)]
    public async Task PublicEndpointBindsLanguageAndLocation(
        string language,
        MenuLocation location)
    {
        var service = new StubService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/navigation/{language}/{location}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(language, service.Language);
        Assert.Equal(location, service.Location);
        var items = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<PublicMenuItem>>();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task PublicEndpointReturnsSortedHierarchyItems()
    {
        var parentId = Guid.NewGuid();
        var service = new StubService
        {
            PublicItems =
            [
                new PublicMenuItem(
                    parentId,
                    null,
                    "Ana menü",
                    "/ana-menu",
                    false,
                    false,
                    1),
                new PublicMenuItem(
                    Guid.NewGuid(),
                    parentId,
                    "Alt menü",
                    "/alt-menu",
                    false,
                    false,
                    2),
            ],
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/navigation/tr/Header");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<PublicMenuItem>>();
        Assert.NotNull(items);
        Assert.Equal(2, items.Count);
        Assert.Null(items[0].ParentId);
        Assert.Equal(parentId, items[1].ParentId);
        Assert.Equal(1, items[0].SortOrder);
        Assert.Equal(2, items[1].SortOrder);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        StubService? service = null) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseSetting(
                        "ConnectionStrings:EkiphanDatabase",
                        "Server=(localdb)\\mssqllocaldb;" +
                        "Database=MenuEndpointTests;" +
                        "Trusted_Connection=True");
                    if (service is not null)
                    {
                        builder.ConfigureTestServices(
                            services =>
                            {
                                services.RemoveAll<IMenuService>();
                                services.AddSingleton<IMenuService>(service);
                            });
                    }
                });

    private sealed class StubService : IMenuService
    {
        public IReadOnlyList<PublicMenuItem> PublicItems { get; init; } = [];

        public string? Language { get; private set; }

        public MenuLocation? Location { get; private set; }

        public Task<IReadOnlyList<AdminMenuItem>> GetAdminItemsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdminMenuItem>>([]);

        public Task<AdminMenuItem> CreateAsync(
            SaveMenuItemCommand command,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdminMenuItem?> UpdateAsync(
            Guid menuItemId,
            SaveMenuItemCommand command,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PublicMenuItem>> GetPublicItemsAsync(
            string languageCode,
            MenuLocation location,
            CancellationToken cancellationToken = default)
        {
            Language = languageCode;
            Location = location;
            return Task.FromResult(PublicItems);
        }
    }
}
