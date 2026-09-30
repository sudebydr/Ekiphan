using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ekiphan.Application.Content;
using Ekiphan.Application.Identity;
using Ekiphan.Domain.Content;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ekiphan.UnitTests.Api;

public sealed class AdminContactEndpointTests
{
    private const string SigningKey =
        "test-only-signing-key-at-least-32-bytes";

    [Fact]
    public async Task ContactReaderCanListWithServerFilters()
    {
        var categoryId = Guid.NewGuid();
        var service = new StubContactService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token("contacts.read"));

        using var response = await client.GetAsync(
            "/api/admin/contact-requests?page=2&pageSize=10" +
            "&status=Read&search=mutfak&sort=Oldest" +
            $"&complaintCategoryId={categoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(categoryId, service.Query?.ComplaintCategoryId);
        Assert.Equal(2, service.Query?.Page);
        Assert.Equal(10, service.Query?.PageSize);
        Assert.Equal(ContactRequestStatus.Read, service.Query?.Status);
        Assert.Equal("mutfak", service.Query?.Search);
        Assert.Equal(AdminContactSortOrder.Oldest, service.Query?.SortOrder);
    }

    [Fact]
    public async Task ContactListRejectsInvalidCategoryFilter()
    {
        var service = new StubContactService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token("contacts.read"));

        using var response = await client.GetAsync(
            "/api/admin/contact-requests?complaintCategoryId=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.Query);
    }

    [Fact]
    public async Task ContactEndpointsRejectWrongPermission()
    {
        await using var factory = CreateFactory(new StubContactService());
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token("catalog.manage"));

        using var response = await client.GetAsync("/api/admin/contact-requests");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ContactReaderCannotMutate()
    {
        await using var factory = CreateFactory(new StubContactService());
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token("contacts.read"));

        using var response = await client.PostAsJsonAsync(
            $"/api/admin/contact-requests/{Guid.NewGuid()}/status",
            new
            {
                status = ContactRequestStatus.Read,
                expectedVersion = Convert.ToBase64String(new byte[8]),
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ContactListContractDoesNotExposeFullContactData()
    {
        var service = new StubContactService
        {
            Page = new AdminContactPage(
                [new AdminContactSummary(
                    Guid.NewGuid(), DateTimeOffset.UtcNow, "Test Kullanıcı",
                    "t***@example.com", "*** 0000", "Bilgi", "Kısa özet",
                    ContactRequestStatus.New, "Müşteri Şikayeti", "Teslimat", null, null, DateTimeOffset.UtcNow,
                    Convert.ToBase64String(new byte[8]))],
                1, 20, 1),
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token("contacts.read"));

        using var response = await client.GetAsync("/api/admin/contact-requests");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<AdminContactPage>();
        Assert.Equal("Müşteri Şikayeti", page!.Items[0].ReasonName);
        Assert.Equal("Teslimat", page.Items[0].ComplaintCategoryName);
        Assert.DoesNotContain("test@example.com", json, StringComparison.Ordinal);
        Assert.DoesNotContain("full message", json, StringComparison.OrdinalIgnoreCase);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IContactRequestService service) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:EkiphanDatabase",
                "Server=(localdb)\\mssqllocaldb;Database=AdminContactTests;Trusted_Connection=True");
            builder.UseSetting("Authentication:Jwt:Issuer", "https://issuer.example");
            builder.UseSetting("Authentication:Jwt:Audience", "ekiphan-admin");
            builder.UseSetting("Authentication:Jwt:SigningKey", SigningKey);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAdminAuthenticationService>();
                services.AddSingleton<IAdminAuthenticationService,
                    TestAdminAuthenticationService>();
                services.RemoveAll<IContactRequestService>();
                services.AddSingleton(service);
            });
        });

    private static string Token(string permission)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "https://issuer.example",
            Audience = "ekiphan-admin",
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(),
                ["jti"] = Guid.NewGuid().ToString("N"),
                ["security_stamp"] = TestAdminAuthenticationService.SecurityStamp,
                ["permission"] = permission,
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private sealed class StubContactService : IContactRequestService
    {
        public AdminContactListQuery? Query { get; private set; }
        public AdminContactPage Page { get; init; } = new([], 1, 20, 0);

        public Task<ContactSubmissionResult> SubmitAsync(
            SubmitContactCommand command, string consentVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ContactSubmissionResult(
                Guid.NewGuid(), DateTimeOffset.UtcNow));

        public Task<AdminContactPage> GetAdminAsync(
            AdminContactListQuery query,
            CancellationToken cancellationToken = default)
        {
            Query = query;
            return Task.FromResult(Page with
            {
                Page = query.Page,
                PageSize = query.PageSize,
            });
        }

        public Task<AdminContactDetail?> GetAdminDetailAsync(
            Guid requestId, CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminContactDetail?>(null);

        public Task<AdminComplaintPage> GetComplaintsAsync(
            AdminComplaintListQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdminComplaintPage(
                [], query.Page, query.PageSize, 0));

        public Task<IReadOnlyList<AdminContactAssignee>> GetAssigneesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdminContactAssignee>>([]);

        public Task<AdminContactMutationResult> ChangeStatusAsync(
            ChangeContactStatusCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result(command.ContactRequestId, command.Status));

        public Task<AdminContactMutationResult> AssignAsync(
            AssignContactCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result(command.ContactRequestId, ContactRequestStatus.New));

        public Task<AdminContactMutationResult> AddNoteAsync(
            AddContactNoteCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result(command.ContactRequestId, ContactRequestStatus.New));

        private static AdminContactMutationResult Result(
            Guid id, ContactRequestStatus status) =>
            new(id, status, null, DateTimeOffset.UtcNow,
                Convert.ToBase64String(new byte[8]));
    }
}
