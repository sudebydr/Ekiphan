using Ekiphan.Domain.Identity;

namespace Ekiphan.UnitTests.Identity;

public sealed class AdminUserTests
{
    [Fact]
    public void ProfileAndPermissionsAreNormalized()
    {
        var user = new AdminUser(
            Guid.NewGuid(),
            " Admin@Example.com ",
            " Administrator ");

        user.SetPermissions(
            [
                AdminPermissionCode.CatalogManage,
                AdminPermissionCode.QuotesManage,
            ]);

        Assert.Equal("admin@example.com", user.Email);
        Assert.Equal("ADMIN@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal("Administrator", user.DisplayName);
        Assert.Equal(2, user.Permissions.Count);
    }

    [Fact]
    public void FiveFailedLoginsLockAccountForFifteenMinutes()
    {
        var user = new AdminUser(
            Guid.NewGuid(),
            "admin@example.com",
            "Administrator");
        var now = DateTimeOffset.UtcNow;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            user.RegisterFailedLogin(now);
        }

        Assert.True(user.IsLockedOut(now.AddMinutes(14)));
        Assert.False(user.IsLockedOut(now.AddMinutes(16)));
        Assert.Equal(0, user.AccessFailedCount);
    }

    [Fact]
    public void SuccessfulLoginClearsFailureState()
    {
        var user = new AdminUser(
            Guid.NewGuid(),
            "admin@example.com",
            "Administrator");
        var now = DateTimeOffset.UtcNow;
        user.RegisterFailedLogin(now);

        user.RegisterSuccessfulLogin(now.AddMinutes(1));

        Assert.Equal(0, user.AccessFailedCount);
        Assert.Null(user.LockoutEnd);
        Assert.Equal(now.AddMinutes(1), user.LastLoginAt);
    }

    [Fact]
    public void ActiveStateChangeRotatesSecurityStamp()
    {
        var user = new AdminUser(
            Guid.NewGuid(),
            "admin@example.com",
            "Administrator");
        var originalStamp = user.SecurityStamp;

        user.SetActive(false);

        Assert.False(user.IsActive);
        Assert.NotEqual(originalStamp, user.SecurityStamp);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase123!")]
    [InlineData("ALLUPPERCASE123!")]
    [InlineData("NoDigitsInPassword!")]
    [InlineData("NoSymbolsInPassword123")]
    public void WeakPasswordIsRejected(string password)
    {
        Assert.Throws<ArgumentException>(
            () => AdminPasswordPolicy.EnsureStrong(password));
    }
}
