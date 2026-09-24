using System.Security.Cryptography;
using Ekiphan.Application.Identity;
using Ekiphan.Domain.Quotes;

namespace Ekiphan.Application.Quotes;

public sealed class QuoteManagementService(
    IQuoteManagementRepository repository,
    IQuoteStatusTransitionService transitionService,
    IQuoteActivityService activityService,
    IAuditLogService auditLogService,
    TimeProvider timeProvider)
{
    public async Task<ChangeQuoteStatusResult> ChangeStatusAsync(
        ChangeQuoteStatusCommand command,
        QuoteRequestContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.QuoteId == Guid.Empty ||
            command.ChangedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Identifiers cannot be empty.", nameof(command));
        }

        if (!Enum.IsDefined(command.Status))
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        if (command.ExpectedVersion is not { Length: 8 })
        {
            throw new ArgumentException(
                "Expected row version must contain 8 bytes.",
                nameof(command));
        }

        var quote = await repository.GetAsync(
            command.QuoteId,
            cancellationToken);
        if (quote is null)
        {
            throw new QuoteNotFoundException(command.QuoteId);
        }

        if (quote.RowVersion.Length != command.ExpectedVersion.Length ||
            !CryptographicOperations.FixedTimeEquals(
                quote.RowVersion,
                command.ExpectedVersion))
        {
            throw new QuoteConcurrencyException();
        }

        if (!transitionService.CanTransition(quote.Status, command.Status))
        {
            throw new InvalidOperationException($"Transition from '{quote.Status}' to '{command.Status}' is invalid.");
        }

        var previousStatus = quote.Status;
        quote.TransitionTo(
            command.Status,
            timeProvider.GetUtcNow(),
            command.ChangedByUserId,
            command.Note);

        await repository.SaveChangesAsync(cancellationToken);

        var ctx = context ?? new QuoteRequestContext(null, null, Guid.NewGuid().ToString("N"));
        await activityService.LogAsync(
            quote.Id,
            QuoteActivityType.StatusChanged,
            command.ChangedByUserId,
            previousStatus.ToString(),
            command.Status.ToString(),
            $"Quote status updated from {previousStatus} to {command.Status}.",
            new { Reason = command.Note },
            ctx,
            cancellationToken);

        await auditLogService.WriteAsync(
            command.ChangedByUserId,
            "ChangeStatus",
            "Quote",
            "QuoteRequest",
            quote.Id.ToString(),
            null,
            new { Status = previousStatus },
            new { Status = command.Status, Note = command.Note },
            command.Note,
            new AdminSecurityContext(ctx.IpAddress, ctx.UserAgent, ctx.CorrelationId),
            true,
            null,
            cancellationToken);

        return new ChangeQuoteStatusResult(
            quote.Id,
            quote.Status,
            quote.AssignedToUserId,
            quote.UpdatedAt,
            Convert.ToBase64String(quote.RowVersion));
    }

    public async Task<ChangeQuoteStatusResult> AssignAsync(
        AssignQuoteCommand command,
        QuoteRequestContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateIdentifiersAndVersion(
            command.QuoteId,
            command.ChangedByUserId,
            command.ExpectedVersion);
        if (command.AssignedToUserId.HasValue &&
            !await repository.CanAssignUserAsync(
                command.AssignedToUserId.Value,
                cancellationToken))
        {
            throw new ArgumentException(
                "Assigned user is not an active quote operator.",
                nameof(command));
        }

        var quote = await GetCurrentAsync(
            command.QuoteId,
            command.ExpectedVersion,
            cancellationToken);
        var previousAssignee = quote.AssignedToUserId;
        quote.AssignTo(command.AssignedToUserId);
        quote.AddInternalNote(
            command.ChangedByUserId,
            command.AssignedToUserId.HasValue
                ? "Quote assigned to an operator."
                : "Quote assignment removed.",
            timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        var ctx = context ?? new QuoteRequestContext(null, null, Guid.NewGuid().ToString("N"));
        await activityService.LogAsync(
            quote.Id,
            command.AssignedToUserId.HasValue ? QuoteActivityType.Assigned : QuoteActivityType.Unassigned,
            command.ChangedByUserId,
            previousAssignee?.ToString(),
            command.AssignedToUserId?.ToString(),
            command.AssignedToUserId.HasValue ? "Quote assigned to user." : "Quote assignment removed.",
            new { AssignedToUserId = command.AssignedToUserId },
            ctx,
            cancellationToken);

        return Result(quote);
    }

    public async Task<ChangeQuoteStatusResult> AddNoteAsync(
        AddQuoteNoteCommand command,
        QuoteRequestContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateIdentifiersAndVersion(
            command.QuoteId,
            command.AuthorUserId,
            command.ExpectedVersion);
        var quote = await GetCurrentAsync(
            command.QuoteId,
            command.ExpectedVersion,
            cancellationToken);
        var note = quote.AddInternalNote(
            command.AuthorUserId,
            command.Text,
            timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        var ctx = context ?? new QuoteRequestContext(null, null, Guid.NewGuid().ToString("N"));
        await activityService.LogAsync(
            quote.Id,
            QuoteActivityType.NoteAdded,
            command.AuthorUserId,
            null,
            note.Id.ToString(),
            "Internal note added.",
            new { NoteId = note.Id },
            ctx,
            cancellationToken);

        return Result(quote);
    }

    public async Task<ChangeQuoteStatusResult> UpdateNoteAsync(
        UpdateQuoteNoteCommand command,
        QuoteRequestContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateIdentifiersAndVersion(
            command.QuoteId,
            command.AuthorUserId,
            command.ExpectedVersion);

        var quote = await GetCurrentAsync(
            command.QuoteId,
            command.ExpectedVersion,
            cancellationToken);

        var note = quote.InternalNotes.FirstOrDefault(x => x.Id == command.NoteId && !x.IsDeleted);
        if (note is null)
        {
            throw new KeyNotFoundException("Note not found or deleted.");
        }

        note.Update(command.Text, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        var ctx = context ?? new QuoteRequestContext(null, null, Guid.NewGuid().ToString("N"));
        await activityService.LogAsync(
            quote.Id,
            QuoteActivityType.NoteUpdated,
            command.AuthorUserId,
            null,
            note.Id.ToString(),
            "Internal note updated.",
            new { NoteId = note.Id },
            ctx,
            cancellationToken);

        return Result(quote);
    }

    public async Task<ChangeQuoteStatusResult> DeleteNoteAsync(
        DeleteQuoteNoteCommand command,
        QuoteRequestContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateIdentifiersAndVersion(
            command.QuoteId,
            command.DeletedByUserId,
            command.ExpectedVersion);

        var quote = await GetCurrentAsync(
            command.QuoteId,
            command.ExpectedVersion,
            cancellationToken);

        var note = quote.InternalNotes.FirstOrDefault(x => x.Id == command.NoteId && !x.IsDeleted);
        if (note is null)
        {
            throw new KeyNotFoundException("Note not found or deleted.");
        }

        note.SoftDelete(command.DeletedByUserId, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        var ctx = context ?? new QuoteRequestContext(null, null, Guid.NewGuid().ToString("N"));
        await activityService.LogAsync(
            quote.Id,
            QuoteActivityType.NoteDeleted,
            command.DeletedByUserId,
            note.Id.ToString(),
            null,
            "Internal note deleted.",
            new { NoteId = note.Id },
            ctx,
            cancellationToken);

        return Result(quote);
    }

    private async Task<QuoteRequest> GetCurrentAsync(
        Guid quoteId,
        byte[] expectedVersion,
        CancellationToken cancellationToken)
    {
        var quote = await repository.GetAsync(quoteId, cancellationToken);
        if (quote is null)
        {
            throw new QuoteNotFoundException(quoteId);
        }
        if (quote.RowVersion.Length != expectedVersion.Length ||
            !CryptographicOperations.FixedTimeEquals(
                quote.RowVersion,
                expectedVersion))
        {
            throw new QuoteConcurrencyException();
        }
        return quote;
    }

    private static void ValidateIdentifiersAndVersion(
        Guid quoteId,
        Guid actorUserId,
        byte[] expectedVersion)
    {
        if (quoteId == Guid.Empty || actorUserId == Guid.Empty)
        {
            throw new ArgumentException("Identifiers cannot be empty.");
        }
        if (expectedVersion is not { Length: 8 })
        {
            throw new ArgumentException(
                "Expected row version must contain 8 bytes.");
        }
    }

    private static ChangeQuoteStatusResult Result(
        QuoteRequest quote) =>
        new(
            quote.Id,
            quote.Status,
            quote.AssignedToUserId,
            quote.UpdatedAt,
            Convert.ToBase64String(quote.RowVersion));
}
