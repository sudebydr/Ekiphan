using Ekiphan.Application.Quotes;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Ekiphan.Api.Quotes;

internal static class QuoteEndpoints
{
    private const long MaximumRequestBodySize = 64 * 1024;

    public static IEndpointConventionBuilder MapQuoteSubmission(
        this IEndpointRouteBuilder endpoints,
        QuoteConsentSettings consentSettings)
    {
        return endpoints.MapPost("/api/quotes", SubmitAsync)
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBodySize))
            .RequireRateLimiting("quote-submit");
    }

    private static async Task<IResult> SubmitAsync(
        [FromBody] SubmitQuoteCommand command,
        HttpContext context,
        Microsoft.Extensions.Options.IOptions<QuoteConsentSettings> consentOptions,
        IValidator<SubmitQuoteCommand> validator,
        QuoteSubmissionService service,
        CancellationToken cancellationToken)
    {
        var consentSettings = consentOptions.Value;
        if (!consentSettings.IsConfigured)
        {
            return Problem(
                StatusCodes.Status503ServiceUnavailable,
                "Quote submission is unavailable until consent versions are configured.");
        }

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var requestContext = ExtractRequestContext(context);
            var response = await service.SubmitAsync(
                command,
                consentSettings.KvkkVersion!,
                consentSettings.CommercialCommunicationVersion!,
                requestContext,
                cancellationToken);

            return Results.Accepted(
                $"/api/quotes/{response.RequestNumber}",
                response);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                exception.Message);
        }
    }

    private static QuoteRequestContext ExtractRequestContext(HttpContext httpContext)
    {
        string? ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) &&
            !string.IsNullOrWhiteSpace(forwardedFor.FirstOrDefault()))
        {
            ipAddress = forwardedFor.FirstOrDefault()?.Split(',')[0].Trim();
        }

        string? userAgent = httpContext.Request.Headers.UserAgent.ToString();
        string? botVerificationToken = httpContext.Request.Headers["X-Bot-Protection-Token"].FirstOrDefault();
        string? idempotencyKey = httpContext.Request.Headers["X-Idempotency-Key"].FirstOrDefault();

        return new QuoteRequestContext(
            ipAddress,
            userAgent,
            httpContext.TraceIdentifier,
            botVerificationToken,
            idempotencyKey);
    }

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(statusCode: statusCode, detail: detail);
}
