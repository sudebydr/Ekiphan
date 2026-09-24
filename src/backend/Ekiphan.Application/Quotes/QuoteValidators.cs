using FluentValidation;

namespace Ekiphan.Application.Quotes;

public sealed class AssignQuoteCommandValidator : AbstractValidator<AssignQuoteCommand>
{
    public AssignQuoteCommandValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).NotEmpty();
        RuleFor(x => x.ChangedByUserId).NotEmpty();
    }
}

public sealed class ChangeQuoteStatusCommandValidator : AbstractValidator<ChangeQuoteStatusCommand>
{
    public ChangeQuoteStatusCommandValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.ExpectedVersion).NotEmpty();
        RuleFor(x => x.ChangedByUserId).NotEmpty();

        RuleFor(x => x.Note)
            .NotEmpty()
            .When(x => x.Status == Domain.Quotes.QuoteStatus.Lost ||
                       x.Status == Domain.Quotes.QuoteStatus.Rejected ||
                       x.Status == Domain.Quotes.QuoteStatus.Cancelled)
            .WithMessage("Reason note is required when rejecting or cancelling a quote.");
    }
}

public sealed class AddQuoteNoteCommandValidator : AbstractValidator<AddQuoteNoteCommand>
{
    public AddQuoteNoteCommandValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ExpectedVersion).NotEmpty();
        RuleFor(x => x.AuthorUserId).NotEmpty();
    }
}

public sealed class UpdateQuoteNoteCommandValidator : AbstractValidator<UpdateQuoteNoteCommand>
{
    public UpdateQuoteNoteCommandValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.NoteId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ExpectedVersion).NotEmpty();
        RuleFor(x => x.AuthorUserId).NotEmpty();
    }
}

public sealed class ArchiveQuoteCommandValidator : AbstractValidator<ArchiveQuoteCommand>
{
    public ArchiveQuoteCommandValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ExpectedVersion).NotEmpty();
        RuleFor(x => x.ArchivedByUserId).NotEmpty();
    }
}

public sealed class PermanentDeleteQuoteCommandValidator : AbstractValidator<PermanentDeleteQuoteCommand>
{
    public PermanentDeleteQuoteCommandValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ReAuthToken).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DeletedByUserId).NotEmpty();
    }
}

public sealed class SubmitQuoteCommandValidator : AbstractValidator<SubmitQuoteCommand>
{
    public SubmitQuoteCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Sector).MaximumLength(150);
        RuleFor(x => x.ProjectName).MaximumLength(200);
        RuleFor(x => x.Message).MaximumLength(4000);
        RuleFor(x => x.LanguageCode).NotEmpty().Must(x => x is "tr" or "en");
        RuleFor(x => x.KvkkConsent).Equal(true).WithMessage("KVKK consent is required.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Quote request must contain at least one item.")
            .Must(x => x.Count <= 50).WithMessage("Quote request cannot contain more than 50 items.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i).Must(i =>
                    (i.ProductId.HasValue && i.ProductId.Value != Guid.Empty) ||
                    (!i.ProductId.HasValue &&
                     !string.IsNullOrWhiteSpace(i.ProductName) &&
                     !string.IsNullOrWhiteSpace(i.Sku)))
                .WithMessage("Each quote item must contain a valid product or product snapshot.");
            item.RuleFor(i => i.ProductName).MaximumLength(300);
            item.RuleFor(i => i.Sku).MaximumLength(150);
            item.RuleFor(i => i.Brand).MaximumLength(150);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.Note).MaximumLength(1000);
        });
    }
}
