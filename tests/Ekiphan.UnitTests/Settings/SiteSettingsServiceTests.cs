using Ekiphan.Application.Settings;
using Ekiphan.Application.Settings.Models;
using Ekiphan.Domain.Settings;

namespace Ekiphan.UnitTests.Settings;

public sealed class SiteSettingsServiceTests
{
    private readonly StubRepo _repositoryStub;
    private readonly SiteSettingsService _service;

    public SiteSettingsServiceTests()
    {
        _repositoryStub = new StubRepo();
        _service = new SiteSettingsService(_repositoryStub);
    }

    [Fact]
    public async Task GetSettingsAsyncReturnsDto()
    {
        var result = await _service.GetSettingsAsync();
        Assert.NotNull(result);
        Assert.Equal("phone", result.Phone);
    }

    [Fact]
    public async Task UpdateSettingsAsyncWithValidRequestUpdatesSettings()
    {
        var request = new UpdateSiteSettingsRequest(
            "new_phone", "new_fax", "valid@test.com", "addr1", "addr2", "addr3", 
            "https://instagram.com/ekiphan", "https://linkedin.com/ekiphan", "https://youtu.be/test", 
            "title", "slogan", "https://tour.com", Convert.ToBase64String(_repositoryStub.Settings.RowVersion));

        var result = await _service.UpdateSettingsAsync(request);

        Assert.Equal("new_phone", result.Phone);
        Assert.Equal("valid@test.com", result.ContactEmail);
        Assert.True(_repositoryStub.Saved);
    }

    [Fact]
    public async Task UpdateSettingsAsyncWithInvalidInstagramUrlThrowsArgumentException()
    {
        var request = new UpdateSiteSettingsRequest(
            "new_phone", "new_fax", "valid@test.com", "addr1", "addr2", "addr3", 
            "https://fakeinstagram.com/ekiphan", null, null, 
            "title", "slogan", null, Convert.ToBase64String(_repositoryStub.Settings.RowVersion));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateSettingsAsync(request));
        Assert.Contains("Instagram domain", ex.Message);
    }

    [Fact]
    public async Task UpdateSettingsAsyncWithHttpUrlThrowsArgumentException()
    {
        var request = new UpdateSiteSettingsRequest(
            "new_phone", "new_fax", "valid@test.com", "addr1", "addr2", "addr3", 
            "http://instagram.com/ekiphan", null, null, 
            "title", "slogan", null, Convert.ToBase64String(_repositoryStub.Settings.RowVersion));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateSettingsAsync(request));
        Assert.Contains("HTTPS absolute URL", ex.Message);
    }

    [Fact]
    public async Task UpdateSettingsAsyncWithUserInfoInUrlThrowsArgumentException()
    {
        var request = new UpdateSiteSettingsRequest(
            "new_phone", "new_fax", "valid@test.com", "addr1", "addr2", "addr3", 
            "https://user:pass@instagram.com/ekiphan", null, null, 
            "title", "slogan", null, Convert.ToBase64String(_repositoryStub.Settings.RowVersion));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateSettingsAsync(request));
        Assert.Contains("credentials", ex.Message);
    }

    [Fact]
    public async Task UpdateSettingsAsyncWithInvalidContactEmailThrowsArgumentException()
    {
        var request = new UpdateSiteSettingsRequest(
            "new_phone", "new_fax", "invalid_email_format", "addr1", "addr2", "addr3", 
            null, null, null, 
            "title", "slogan", null, Convert.ToBase64String(_repositoryStub.Settings.RowVersion));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateSettingsAsync(request));
        Assert.Contains("ContactEmail", ex.Message);
    }

    [Fact]
    public async Task UpdateSettingsAsyncWithValidSocialUrlsAccepts()
    {
        var request = new UpdateSiteSettingsRequest(
            "new_phone", "new_fax", "valid@test.com", "addr1", "addr2", "addr3", 
            "https://instagram.com/ekiphan", "https://linkedin.com/company/ekiphan", "https://youtube.com/ekiphan", 
            "title", "slogan", null, Convert.ToBase64String(_repositoryStub.Settings.RowVersion));

        var result = await _service.UpdateSettingsAsync(request);
        Assert.Equal("https://linkedin.com/company/ekiphan", result.LinkedInUrl);
        Assert.Equal("https://youtube.com/ekiphan", result.YouTubeUrl);
    }

    [Fact]
    public async Task UpdateSettingsAsyncWithConcurrencyConflictThrowsInvalidOperationException()
    {
        var request = new UpdateSiteSettingsRequest(
            "new_phone", "new_fax", "valid@test.com", "addr1", "addr2", "addr3", 
            null, null, null, 
            "title", "slogan", null, "invalid_row_version");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateSettingsAsync(request));
    }

    [Fact]
    public async Task UpdateSettingsAsyncWithEmptyUrlAccepts()
    {
        var request = new UpdateSiteSettingsRequest(
            "new_phone", "new_fax", "valid@test.com", "addr1", "addr2", "addr3", 
            "", null, "  ", 
            "title", "slogan", null, Convert.ToBase64String(_repositoryStub.Settings.RowVersion));

        var result = await _service.UpdateSettingsAsync(request);
        Assert.Null(result.InstagramUrl);
        Assert.Null(result.YouTubeUrl);
    }

    private sealed class StubRepo : ISiteSettingsRepository
    {
        public SiteSettings Settings = new SiteSettings(Guid.NewGuid(), "phone", "fax", "email@a.com", "addr1", "addr2", "addr3", null, null, null, "title", "slogan", null);
        public bool Saved;
        public Task<SiteSettings> GetSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Settings);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) { Saved = true; return Task.CompletedTask; }
    }
}
