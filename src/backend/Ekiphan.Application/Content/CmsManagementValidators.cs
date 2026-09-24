using FluentValidation;

namespace Ekiphan.Application.Content;

public sealed class CreateReferenceProjectCommandValidator : AbstractValidator<CreateReferenceProjectCommand>
{
    public CreateReferenceProjectCommandValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Translations).NotEmpty().WithMessage("At least Turkish translation is required.");
        RuleFor(x => x.Translations).Must(t => t.Any(tr => tr.LanguageCode.Equals("tr", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Turkish translation is required.");
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(tr => tr.Title).NotEmpty().MaximumLength(250);
            t.RuleFor(tr => tr.Slug).NotEmpty().MaximumLength(250);
        });
    }
}

public sealed class UpdateReferenceProjectCommandValidator : AbstractValidator<UpdateReferenceProjectCommand>
{
    public UpdateReferenceProjectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Translations).NotEmpty().WithMessage("At least Turkish translation is required.");
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(tr => tr.Title).NotEmpty().MaximumLength(250);
            t.RuleFor(tr => tr.Slug).NotEmpty().MaximumLength(250);
        });
    }
}

public sealed class CreateCustomerLogoCommandValidator : AbstractValidator<CreateCustomerLogoCommand>
{
    public CreateCustomerLogoCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.MediaAssetId).NotEmpty();
        RuleFor(x => x.WebsiteUrl).MaximumLength(1000);
    }
}

public sealed class UpdateCustomerLogoCommandValidator : AbstractValidator<UpdateCustomerLogoCommand>
{
    public UpdateCustomerLogoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.MediaAssetId).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}

public sealed class CreateShowroomCommandValidator : AbstractValidator<CreateShowroomCommand>
{
    public CreateShowroomCommandValidator()
    {
        RuleFor(x => x.Translations).NotEmpty().WithMessage("At least Turkish translation is required.");
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(tr => tr.Title).NotEmpty().MaximumLength(250);
            t.RuleFor(tr => tr.Slug).NotEmpty().MaximumLength(250);
        });
    }
}

public sealed class CreateShowroomHotspotCommandValidator : AbstractValidator<CreateShowroomHotspotCommand>
{
    public CreateShowroomHotspotCommandValidator()
    {
        RuleFor(x => x.ShowroomId).NotEmpty();
        RuleFor(x => x.SceneIdentifier).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PositionX).InclusiveBetween(-1000.0, 1000.0);
        RuleFor(x => x.PositionY).InclusiveBetween(-1000.0, 1000.0);
        RuleFor(x => x.PositionZ).InclusiveBetween(-1000.0, 1000.0);
    }
}

public sealed class CreateBannerGroupCommandValidator : AbstractValidator<CreateBannerGroupCommand>
{
    public CreateBannerGroupCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

public sealed class CreateBannerCommandValidator : AbstractValidator<CreateBannerCommand>
{
    public CreateBannerCommandValidator()
    {
        RuleFor(x => x.BannerGroupId).NotEmpty();
        RuleFor(x => x.DesktopMediaAssetId).NotEmpty();
        RuleFor(x => x.LinkUrl).MaximumLength(1000);
        RuleFor(x => x).Must(x => !x.PublishAt.HasValue || !x.PublishEndAt.HasValue || x.PublishEndAt.Value > x.PublishAt.Value)
            .WithMessage("PublishEndAt must be after PublishAt.");
    }
}

public sealed class UpdateSiteSettingsCommandValidator : AbstractValidator<UpdateSiteSettingsCommand>
{
    public UpdateSiteSettingsCommandValidator()
    {
        RuleFor(x => x.Settings).NotEmpty();
        RuleForEach(x => x.Settings).ChildRules(s =>
        {
            s.RuleFor(item => item.Key).NotEmpty();
            s.RuleFor(item => item.RowVersion).NotEmpty();
        });
    }
}

public sealed class CreateFooterColumnCommandValidator : AbstractValidator<CreateFooterColumnCommand>
{
    public CreateFooterColumnCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TitleTr).NotEmpty().MaximumLength(150);
    }
}

public sealed class CreateFooterLinkCommandValidator : AbstractValidator<CreateFooterLinkCommand>
{
    public CreateFooterLinkCommandValidator()
    {
        RuleFor(x => x.FooterColumnId).NotEmpty();
        RuleFor(x => x.LabelTr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(1000);
    }
}

public sealed class ReorderCmsItemsCommandValidator : AbstractValidator<ReorderCmsItemsCommand>
{
    public ReorderCmsItemsCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleFor(x => x.Items).Must(items => items.Select(i => i.Id).Distinct().Count() == items.Count)
            .WithMessage("Duplicate item IDs in reorder payload.");
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(item => item.Id).NotEmpty();
            i.RuleFor(item => item.SortOrder).GreaterThanOrEqualTo(0);
        });
    }
}
