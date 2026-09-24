using Ekiphan.Domain.Content;

namespace Ekiphan.UnitTests.Content;

public sealed class ContactRequestTests
{
    [Fact]
    public void ValidRequestNormalizesEmailAndLanguage()
    {
        var request = new ContactRequest(
            Guid.NewGuid(), "Aysun Test", "AYSUN@example.com", null, null,
            "Bilgi talebi", "Ürünler hakkında bilgi almak istiyorum.", "TR",
            DateTimeOffset.UtcNow, "kvkk-v1");

        Assert.Equal("aysun@example.com", request.Email);
        Assert.Equal("tr", request.LanguageCode);
    }

    [Fact]
    public void InvalidEmailIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new ContactRequest(
            Guid.NewGuid(), "Aysun Test", "gecersiz", null, null,
            "Bilgi", "Mesaj", "tr", DateTimeOffset.UtcNow, "kvkk-v1"));
    }

    [Fact]
    public void ConsentTimestampIsRequired()
    {
        Assert.Throws<ArgumentException>(() => new ContactRequest(
            Guid.NewGuid(), "Aysun Test", "aysun@example.com", null, null,
            "Bilgi", "Mesaj", "tr", default, "kvkk-v1"));
    }

    [Fact]
    public void ValidStatusFlowRecordsActorAndRejectsInvalidTransition()
    {
        var request = new ContactRequest(
            Guid.NewGuid(), "Aysun Test", "aysun@example.com", null, null,
            "Bilgi", "Mesaj", "tr", DateTimeOffset.UtcNow, "kvkk-v1");
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        request.TransitionTo(ContactRequestStatus.Read, actor, now);

        Assert.Equal(ContactRequestStatus.Read, request.Status);
        Assert.Equal(actor, request.StatusHistory.Last().ChangedByUserId);
        Assert.Throws<InvalidOperationException>(() =>
            request.TransitionTo(ContactRequestStatus.New, actor, now));
    }

    [Fact]
    public void AssignmentAndInternalNoteAreStoredWithoutContactData()
    {
        var request = new ContactRequest(
            Guid.NewGuid(), "Aysun Test", "aysun@example.com", null, null,
            "Bilgi", "Mesaj", "tr", DateTimeOffset.UtcNow, "kvkk-v1");
        var actor = Guid.NewGuid();

        request.AssignTo(actor);
        request.AddInternalNote(actor, "Yanıt taslağı hazırlanacak.", DateTimeOffset.UtcNow);

        Assert.Equal(actor, request.AssignedToUserId);
        Assert.Single(request.InternalNotes);
        Assert.DoesNotContain("aysun@example.com", request.InternalNotes.Single().Text);
    }
}
