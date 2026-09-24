using Ekiphan.Domain.Identity;

namespace Ekiphan.UnitTests.Identity;

public sealed class AdminSessionTests
{
    [Fact]
    public void ActiveSessionCanBeTouchedWithinIdleWindow()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = new AdminSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            createdAt,
            createdAt.AddHours(8));

        session.Touch(createdAt.AddMinutes(20));

        Assert.True(
            session.IsValid(
                createdAt.AddMinutes(49),
                TimeSpan.FromMinutes(30)));
        Assert.False(
            session.IsValid(
                createdAt.AddMinutes(50),
                TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void RevokedSessionCannotBeUsedOrTouched()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = new AdminSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            createdAt,
            createdAt.AddHours(1));
        session.Revoke(createdAt.AddMinutes(1));

        Assert.False(
            session.IsValid(
                createdAt.AddMinutes(2),
                TimeSpan.FromMinutes(30)));
        Assert.Throws<InvalidOperationException>(
            () => session.Touch(createdAt.AddMinutes(2)));
    }

    [Fact]
    public void AbsoluteExpirationCannotBeExtendedByActivity()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = new AdminSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            createdAt,
            createdAt.AddMinutes(60));
        session.Touch(createdAt.AddMinutes(59));

        Assert.False(
            session.IsValid(
                createdAt.AddMinutes(60),
                TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void SessionTimesAreNormalizedToDatabasePrecisionAndUtc()
    {
        var createdAt = new DateTimeOffset(
            2026,
            7,
            31,
            15,
            0,
            0,
            987,
            TimeSpan.FromHours(3));
        var session = new AdminSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            createdAt,
            createdAt.AddHours(1));

        Assert.Equal(TimeSpan.Zero, session.LastActivityAt.Offset);
        Assert.Equal(0, session.LastActivityAt.Ticks % TimeSpan.TicksPerSecond);
        Assert.Equal(0, session.ExpiresAt.Ticks % TimeSpan.TicksPerSecond);
    }

    [Fact]
    public void ImmediateTouchAcceptsSqlServerSecondRoundingDifference()
    {
        var roundedDatabaseTime = new DateTimeOffset(
            2026,
            7,
            31,
            12,
            0,
            1,
            TimeSpan.Zero);
        var session = new AdminSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            roundedDatabaseTime,
            roundedDatabaseTime.AddHours(1));

        session.Touch(roundedDatabaseTime.AddMilliseconds(-100));

        Assert.Equal(roundedDatabaseTime, session.LastActivityAt);
        Assert.True(
            session.IsValid(
                roundedDatabaseTime,
                TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void TouchRejectsClockRollbackBeyondStoragePrecision()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = new AdminSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            createdAt,
            createdAt.AddHours(1));

        Assert.Throws<InvalidOperationException>(
            () => session.Touch(createdAt.AddSeconds(-2)));
    }
}
