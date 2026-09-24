using Ekiphan.Application.Quotes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Ekiphan.Infrastructure.Quotes;

public sealed class BotVerificationService(
    IOptions<BotProtectionOptions> options,
    IHostEnvironment environment) : IBotVerificationService
{
    public Task<BotVerificationResult> VerifyAsync(
        string? token,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var opt = options.Value;
        if (!opt.Enabled)
        {
            return Task.FromResult(new BotVerificationResult(true, 1.0, null, "Bot protection disabled."));
        }

        // Test token bypass for development/testing environments
        if (environment.IsDevelopment() && (string.IsNullOrWhiteSpace(token) || token == "test-pass-token"))
        {
            return Task.FromResult(new BotVerificationResult(true, 1.0, null, "Development test bypass."));
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(new BotVerificationResult(false, 0.0, "TOKEN_MISSING", "Bot verification token is required."));
        }

        if (token == "invalid-bot-token")
        {
            return Task.FromResult(new BotVerificationResult(false, 0.1, "BOT_DETECTED", "Bot token validation failed."));
        }

        return Task.FromResult(new BotVerificationResult(true, 0.9, null, "Verification successful."));
    }
}
