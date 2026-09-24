using Ekiphan.Domain.Identity;
using FluentValidation;

namespace Ekiphan.Application.Identity;

public sealed class AdminSecurityOptions { public const string SectionName="AdminSecurity"; public bool RequireTwoFactorForAllAdmins{get;set;} public bool RequireTwoFactorForSuperAdmin{get;set;}=true; public int TwoFactorSetupTokenLifetimeMinutes{get;set;}=10; public int TwoFactorLoginTokenLifetimeMinutes{get;set;}=5; public int TwoFactorMaxAttempts{get;set;}=5; public int TwoFactorLockoutMinutes{get;set;}=15; public int TotpAllowedTimeStepDrift{get;set;}=1; public int RecoveryCodeCount{get;set;}=10; public int ReAuthenticationTokenLifetimeMinutes{get;set;}=5; public int PasswordResetTokenLifetimeMinutes{get;set;}=30; public int PasswordHistoryCount{get;set;}=5; }

public sealed record AdminSecurityContext(string? IpAddress,string? UserAgent,string CorrelationId,Guid? SessionId=null);
public sealed record SetupTwoFactorResultDto(string SetupToken,string OtpAuthUri,string ManualEntryKey,DateTimeOffset ExpiresAt,string Issuer,string AccountName);
public sealed record ConfirmTwoFactorResultDto(bool IsEnabled,IReadOnlyList<string> RecoveryCodes);
public sealed record TwoFactorChallengeDto(string TwoFactorToken,DateTimeOffset ExpiresAt,IReadOnlyList<string> AllowedMethods);
public sealed record VerifyTwoFactorResultDto(AdminAuthenticatedUser User);
public sealed record AdminSessionDto(Guid SessionId,string DeviceName,string? Browser,string? OperatingSystem,string IpAddressMasked,DateTimeOffset CreatedAt,DateTimeOffset LastSeenAt,DateTimeOffset ExpiresAt,bool IsCurrent,string Status);
public sealed record LoginAttemptDto(Guid Id,DateTimeOffset AttemptedAt,bool WasSuccessful,string FailureReason,string IpAddressMasked,string? Device,string RiskLevel);
public sealed record AuditLogListItemDto(Guid Id,Guid? ActorUserId,string Action,string Category,string EntityType,string? EntityId,Guid? TargetUserId,string? Reason,string IpAddressMasked,string CorrelationId,bool WasSuccessful,DateTimeOffset CreatedAt);
public sealed record AuditLogDetailDto(AuditLogListItemDto Item,string? OldValues,string? NewValues,string? FailureCode,string? UserAgent);
public sealed record SecurityEventDto(Guid Id,Guid? UserId,string EventType,string Severity,string Description,bool IsReviewed,DateTimeOffset CreatedAt);
public sealed record SecurityDashboardDto(int Last24HoursLoginCount,int FailedLoginCount,int LockedUserCount,int ActiveSessionCount,int UsersWithoutTwoFactorCount,IReadOnlyList<SecurityEventDto> RecentHighRiskEvents,int RecentRevokedSessions,int PasswordResetRequestCount,int CriticalSecurityEventCount);
public sealed record SetupTwoFactorCommand(Guid UserId);
public sealed record ConfirmTwoFactorCommand(Guid UserId,string SetupToken,string TotpCode);
public sealed record DisableTwoFactorCommand(Guid UserId,string CurrentPassword,string Code,string Reason);
public sealed record RegenerateRecoveryCodesCommand(Guid UserId,string CurrentPassword,string TotpCode);
public sealed record VerifyTwoFactorLoginCommand(string TwoFactorToken,string Code,string Method);
public sealed record ChangePasswordCommand(Guid UserId,string CurrentPassword,string NewPassword,string ConfirmNewPassword,string? TotpCode,Guid CurrentSessionId);
public sealed record ForgotPasswordCommand(string Email);
public sealed record ResetPasswordCommand(string Token,string NewPassword,string ConfirmNewPassword);
public sealed record ReAuthenticateCommand(Guid UserId,string Password,string? TotpCode,string Scope);
public sealed record ReAuthenticationResultDto(string ReAuthToken,DateTimeOffset ExpiresAt,string Scope);

public interface ISecurityTokenHasher { string Hash(string token); string Generate(int bytes=32); }
public interface ITwoFactorService { string GenerateSecret(); string Protect(string secret); string Unprotect(string protectedSecret); bool Verify(string secret,string code,int drift); string BuildOtpAuthUri(string issuer,string account,string secret); }
public interface ITwoFactorTokenService { Task<string> CreateAsync(Guid userId,SecurityTokenPurpose purpose,string? scope,TimeSpan lifetime,CancellationToken ct); Task<Guid?> ValidateAsync(string token,SecurityTokenPurpose purpose,string? scope,CancellationToken ct); Task<Guid?> ConsumeAsync(string token,SecurityTokenPurpose purpose,string? scope,CancellationToken ct); }
public interface IRecoveryCodeService { Task<IReadOnlyList<string>> RegenerateAsync(Guid userId,CancellationToken ct); Task<bool> ConsumeAsync(Guid userId,string code,CancellationToken ct); Task RevokeAsync(Guid userId,CancellationToken ct); }
public interface IUserSecurityService { Task<SetupTwoFactorResultDto> SetupTwoFactorAsync(SetupTwoFactorCommand command,AdminSecurityContext context,CancellationToken ct); Task<ConfirmTwoFactorResultDto> ConfirmTwoFactorAsync(ConfirmTwoFactorCommand command,AdminSecurityContext context,CancellationToken ct); Task DisableTwoFactorAsync(DisableTwoFactorCommand command,AdminSecurityContext context,CancellationToken ct); Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(RegenerateRecoveryCodesCommand command,AdminSecurityContext context,CancellationToken ct); Task<TwoFactorChallengeDto> CreateLoginChallengeAsync(Guid userId,CancellationToken ct); Task<VerifyTwoFactorResultDto> VerifyLoginAsync(VerifyTwoFactorLoginCommand command,AdminSecurityContext context,CancellationToken ct); }
public interface IAdminSessionService { Task<IReadOnlyList<AdminSessionDto>> GetAsync(Guid userId,Guid currentSessionId,CancellationToken ct); Task<bool> RevokeAsync(Guid actorUserId,Guid sessionId,bool canRevokeOthers,string reason,CancellationToken ct); Task<int> RevokeOthersAsync(Guid userId,Guid currentSessionId,string reason,CancellationToken ct); Task<int> RevokeUserAsync(Guid actorUserId,Guid userId,string reason,CancellationToken ct); }
public interface ILoginAttemptService { Task RecordAsync(Guid? userId,string normalizedEmail,bool success,LoginFailureReason reason,AdminSecurityContext context,bool twoFactorRequired,CancellationToken ct); Task<IReadOnlyList<LoginAttemptDto>> GetUserHistoryAsync(Guid userId,int page,int pageSize,CancellationToken ct); }
public interface IPasswordSecurityService { Task ChangeAsync(ChangePasswordCommand command,AdminSecurityContext context,CancellationToken ct); }
public interface IPasswordResetService { Task RequestAsync(ForgotPasswordCommand command,AdminSecurityContext context,CancellationToken ct); Task ResetAsync(ResetPasswordCommand command,AdminSecurityContext context,CancellationToken ct); }
public interface IReAuthenticationService { Task<ReAuthenticationResultDto> ReAuthenticateAsync(ReAuthenticateCommand command,CancellationToken ct); Task<bool> ConsumeAsync(string token,Guid userId,string scope,CancellationToken ct); }
public interface IAuditValueSanitizer { object? Sanitize(object? value); }
public interface IAuditLogService { Task WriteAsync(Guid? actor,string action,string category,string entityType,string? entityId,Guid? target,object? oldValues,object? newValues,string? reason,AdminSecurityContext context,bool success,string? failure,CancellationToken ct); Task<IReadOnlyList<AuditLogListItemDto>> ListAsync(int page,int pageSize,CancellationToken ct); Task<AuditLogDetailDto?> GetAsync(Guid id,bool includeDetails,CancellationToken ct); }
public interface ISecurityEventService { Task<IReadOnlyList<SecurityEventDto>> ListAsync(int page,int pageSize,CancellationToken ct); Task<bool> ReviewAsync(Guid eventId,Guid actor,CancellationToken ct); Task<SecurityDashboardDto> DashboardAsync(CancellationToken ct); }
public interface ISecurityRiskEvaluator { SecurityRiskLevel Evaluate(int recentFailures,bool newIp,bool newDevice); }
public interface ISecurityNotificationService { Task QueuePasswordResetAsync(string email,string token,CancellationToken ct); Task QueuePasswordChangedAsync(string email,CancellationToken ct); }

public sealed class ConfirmTwoFactorCommandValidator:AbstractValidator<ConfirmTwoFactorCommand>{public ConfirmTwoFactorCommandValidator(){RuleFor(x=>x.SetupToken).NotEmpty().MaximumLength(500);RuleFor(x=>x.TotpCode).Matches("^[0-9]{6}$");}}
public sealed class VerifyTwoFactorLoginCommandValidator:AbstractValidator<VerifyTwoFactorLoginCommand>{public VerifyTwoFactorLoginCommandValidator(){RuleFor(x=>x.TwoFactorToken).NotEmpty().MaximumLength(500);RuleFor(x=>x.Method).Must(x=>x is "Totp" or "RecoveryCode");RuleFor(x=>x.Code).NotEmpty().MaximumLength(32);}}
public sealed class DisableTwoFactorCommandValidator:AbstractValidator<DisableTwoFactorCommand>{public DisableTwoFactorCommandValidator(){RuleFor(x=>x.CurrentPassword).NotEmpty().MaximumLength(256);RuleFor(x=>x.Code).NotEmpty().MaximumLength(32);RuleFor(x=>x.Reason).NotEmpty().MaximumLength(500);}}
public sealed class ChangePasswordCommandValidator:AbstractValidator<ChangePasswordCommand>{public ChangePasswordCommandValidator(){RuleFor(x=>x.CurrentPassword).NotEmpty().MaximumLength(256);RuleFor(x=>x.NewPassword).NotEmpty().MinimumLength(14).MaximumLength(256).Equal(x=>x.ConfirmNewPassword);}}
public sealed class ForgotPasswordCommandValidator:AbstractValidator<ForgotPasswordCommand>{public ForgotPasswordCommandValidator(){RuleFor(x=>x.Email).NotEmpty().EmailAddress().MaximumLength(254);}}
public sealed class ResetPasswordCommandValidator:AbstractValidator<ResetPasswordCommand>{public ResetPasswordCommandValidator(){RuleFor(x=>x.Token).NotEmpty().MaximumLength(500);RuleFor(x=>x.NewPassword).MinimumLength(14).MaximumLength(256).Equal(x=>x.ConfirmNewPassword);}}
public sealed class ReAuthenticateCommandValidator:AbstractValidator<ReAuthenticateCommand>{public ReAuthenticateCommandValidator(){RuleFor(x=>x.Password).NotEmpty().MaximumLength(256);RuleFor(x=>x.Scope).NotEmpty().MaximumLength(200);}}
