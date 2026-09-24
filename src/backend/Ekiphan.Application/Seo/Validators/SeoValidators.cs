using FluentValidation;
using Ekiphan.Application.Seo;

namespace Ekiphan.Application.Seo.Validators;

public sealed class UpdateRobotsConfigurationCommandValidator : AbstractValidator<UpdateRobotsConfigurationCommand>
{
    public UpdateRobotsConfigurationCommandValidator()
    {
        RuleFor(x => x.EnvironmentName)
            .NotEmpty().WithMessage("EnvironmentName is required.")
            .MaximumLength(50).WithMessage("EnvironmentName cannot exceed 50 characters.");

        RuleFor(x => x.Content)
            .NotNull()
            .MaximumLength(20000).WithMessage("Robots.txt content cannot exceed 20,000 characters.");
    }
}

public sealed class CreateRedirectRuleCommandValidator : AbstractValidator<CreateRedirectRuleCommand>
{
    public CreateRedirectRuleCommandValidator()
    {
        RuleFor(x => x.SourcePath)
            .NotEmpty().WithMessage("SourcePath is required.")
            .Must(path => path.StartsWith('/')).WithMessage("SourcePath must start with '/'.")
            .Must(path => !path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SourcePath cannot contain scheme.");

        RuleFor(x => x.DestinationUrl)
            .NotEmpty().WithMessage("DestinationUrl is required.")
            .Must(dest => !dest.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
                       && !dest.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                       && !dest.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Unsafe destination URL scheme is prohibited.");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.SourcePath.TrimEnd('/'), x.DestinationUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            .WithMessage("Source and destination cannot be identical.");

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0).WithMessage("Priority cannot be negative.");
    }
}

public sealed class UpdateRedirectRuleCommandValidator : AbstractValidator<UpdateRedirectRuleCommand>
{
    public UpdateRedirectRuleCommandValidator()
    {
        RuleFor(x => x.SourcePath)
            .NotEmpty().WithMessage("SourcePath is required.")
            .Must(path => path.StartsWith('/')).WithMessage("SourcePath must start with '/'.");

        RuleFor(x => x.DestinationUrl)
            .NotEmpty().WithMessage("DestinationUrl is required.");

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0).WithMessage("Priority cannot be negative.");
    }
}

public sealed class ValidateRedirectRuleCommandValidator : AbstractValidator<ValidateRedirectRuleCommand>
{
    public ValidateRedirectRuleCommandValidator()
    {
        RuleFor(x => x.SourcePath).NotEmpty().Must(p => p.StartsWith('/'));
        RuleFor(x => x.DestinationUrl).NotEmpty();
    }
}

public sealed class StartBrokenLinkScanCommandValidator : AbstractValidator<BrokenLinkScanRequest>
{
    public StartBrokenLinkScanCommandValidator()
    {
        RuleFor(x => x.MaxPages)
            .GreaterThan(0).LessThanOrEqualTo(50000)
            .When(x => x.MaxPages.HasValue);
    }
}

public sealed class SeoExportRequestValidator : AbstractValidator<SeoExportRequestDto>
{
    public SeoExportRequestValidator()
    {
        RuleFor(x => x.Format)
            .Must(f => string.Equals(f, "csv", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(f, "xlsx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Export format must be either 'csv' or 'xlsx'.");
    }
}
