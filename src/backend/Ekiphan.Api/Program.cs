using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Ekiphan.Api.Authentication;
using Ekiphan.Api.Administration;
using Ekiphan.Api.Catalog;
using Ekiphan.Api.DataImport;
using Ekiphan.Api.Http;
using Ekiphan.Api.Operations;
using Ekiphan.Api.Media;
using Ekiphan.Api.Quotes;
using Ekiphan.Api.Content;
using Ekiphan.Api.Seo;
using Ekiphan.Application.Deployments;
using Ekiphan.Application.Identity;
using Ekiphan.Infrastructure;
using Ekiphan.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ekiphan.Infrastructure.Monitoring;

var builder = WebApplication.CreateBuilder(args);

var productMediaImportSettings = builder.Configuration
    .GetSection(Ekiphan.Application.MediaImport.ProductMediaImportOptions.SectionName)
    .Get<Ekiphan.Application.MediaImport.ProductMediaImportOptions>() ?? new();

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddProblemDetails();
builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });
builder.Services.AddControllers();
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException(
            "Missing required configuration: ConnectionStrings:Redis.");
    }

    redisConnectionString = "localhost:6379";
}

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessHealthCheck>(
        "database",
        tags: ["ready"])
    .AddRedis(redisConnectionString, tags: ["ready"]);
builder.Services.AddInfrastructure(builder.Configuration, redisConnectionString);
builder.Services.Configure<FormOptions>(
    options => options.MultipartBodyLengthLimit =
        Math.Max(Ekiphan.Application.Media.MediaUploadLimits.MaximumRequestBytes,
            productMediaImportSettings.MaxZipBytes + 1024 * 1024));
builder.WebHost.ConfigureKestrel(
    options => options.Limits.MaxRequestBodySize =
        Math.Max(Ekiphan.Application.Media.MediaUploadLimits.MaximumRequestBytes,
            productMediaImportSettings.MaxZipBytes + 1024 * 1024));
builder.Services.AddRateLimiter(
    options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (rejectionContext, cancellationToken) =>
        {
            var response = rejectionContext.HttpContext.Response;
            response.StatusCode = StatusCodes.Status429TooManyRequests;
            response.ContentType = "application/problem+json";
            if (rejectionContext.Lease.TryGetMetadata(
                    MetadataName.RetryAfter,
                    out var retryAfter))
            {
                response.Headers.RetryAfter = Math.Max(
                    1,
                    (int)Math.Ceiling(retryAfter.TotalSeconds))
                    .ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
            }

            await JsonSerializer.SerializeAsync(
                response.Body,
                new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail =
                        "The request rate limit was exceeded. Retry later.",
                },
                cancellationToken: cancellationToken);
        };
        options.AddPolicy(
            "admin-login",
            context => builder.Environment.IsDevelopment()
                ? RateLimitPartition.GetNoLimiter(
                    $"development-admin-login:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}")
                : RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));
        options.AddPolicy(
            "quote-admin-read",
            context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue("sub") ??
                    context.Connection.RemoteIpAddress?.ToString() ??
                    "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
        options.AddPolicy(
            "quote-admin-write",
            context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue("sub") ??
                    context.Connection.RemoteIpAddress?.ToString() ??
                    "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
        options.AddPolicy(
            "catalog-admin-read",
            context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue("sub") ??
                    context.Connection.RemoteIpAddress?.ToString() ??
                    "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
        options.AddPolicy(
            "catalog-admin-write",
            context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue("sub") ??
                    context.Connection.RemoteIpAddress?.ToString() ??
                    "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
        options.AddPolicy(
            "quote-submit",
            context => builder.Environment.IsDevelopment()
                ? RateLimitPartition.GetNoLimiter("development-quote-submit")
                : RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));        options.AddPolicy(
            "catalog-read",
            context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
        options.AddPolicy(
            "import-upload",
            context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    context.User.FindFirstValue("sub") ??
                    context.Connection.RemoteIpAddress?.ToString() ??
                    "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
        options.AddPolicy(
            "import-read",
            context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    context.User.FindFirstValue("sub") ??
                    context.Connection.RemoteIpAddress?.ToString() ??
                    "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
        options.AddPolicy(
            "media-upload",
            context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    context.User.FindFirstValue("sub") ??
                    context.Connection.RemoteIpAddress?.ToString() ??
                    "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
    });

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? new JwtSettings();
var quoteConsentSettings = builder.Configuration
    .GetSection(QuoteConsentSettings.SectionName)
    .Get<QuoteConsentSettings>() ?? new QuoteConsentSettings();
builder.Services.Configure<QuoteConsentSettings>(builder.Configuration.GetSection(QuoteConsentSettings.SectionName));
builder.Services.AddSingleton(quoteConsentSettings);
if (jwtSettings.IsConfigured)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(
            options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SigningKey!)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var principal = context.Principal;
                        if (principal is null ||
                            !Guid.TryParse(
                                principal.FindFirstValue("sub"),
                                out var userId) ||
                            !Guid.TryParse(
                                principal.FindFirstValue("jti"),
                                out var sessionId))
                        {
                            context.Fail("The admin session claims are invalid.");
                            return;
                        }

                        var securityStamp = principal.FindFirstValue(
                            "security_stamp");
                        if (string.IsNullOrWhiteSpace(securityStamp))
                        {
                            context.Fail("The admin security stamp is missing.");
                            return;
                        }

                        var authenticationService = context.HttpContext
                            .RequestServices
                            .GetRequiredService<IAdminAuthenticationService>();
                        var authenticatedUser = await authenticationService
                            .ValidateSessionAsync(
                                userId,
                                sessionId,
                                securityStamp,
                                TimeSpan.FromMinutes(
                                    jwtSettings.IdleTimeoutMinutes),
                                context.HttpContext.RequestAborted);
                        if (authenticatedUser is null)
                        {
                            context.Fail("The admin session is no longer valid.");
                            return;
                        }

                        context.HttpContext.Items[
                            AdminAuthenticationEndpoints
                                .AuthenticatedUserItemKey] = authenticatedUser;
                    },
                };
            });
    builder.Services.AddAuthorization(
        options =>
        {
            options.AddPolicy(
                "QuoteRead",
                policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
                    context.User.HasClaim("permission", "quotes.read") ||
                    context.User.HasClaim("permission", "quotes.manage")));
            options.AddPolicy(
                "QuoteManage",
                policy => policy.RequireAuthenticatedUser().RequireClaim("permission", "quotes.manage"));
            options.AddPolicy(
                "ContactRead",
                policy => policy.RequireAssertion(context =>
                    context.User.HasClaim("permission", "contacts.read") ||
                    context.User.HasClaim("permission", "contacts.manage")));
            options.AddPolicy(
                "ContactManage",
                policy => policy.RequireClaim("permission", "contacts.manage"));
            options.AddPolicy(
                "ImportManage",
                policy => policy.RequireClaim("permission", "imports.manage"));
            options.AddPolicy(
                "ImportPublish",
                policy => policy.RequireClaim("permission", "imports.publish"));
            options.AddPolicy(
                "ProductsImport",
                policy => policy.RequireClaim("permission", "products.import"));
            options.AddPolicy(
                "ProductsImportRollback",
                policy => policy.RequireClaim("permission", "products.import.rollback"));
            options.AddPolicy(
                "CatalogManage",
                policy => policy.RequireClaim("permission", "catalog.manage"));
            options.AddPolicy("ProductsPublish", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "products.publish") || context.User.HasClaim("permission", "catalog.manage")));
            options.AddPolicy("ProductsPublishDirect", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "products.publish.direct") || context.User.HasClaim("permission", "catalog.manage")));
            options.AddPolicy("ProductsArchive", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "products.archive") || context.User.HasClaim("permission", "catalog.manage")));
            options.AddPolicy("ProductsBulkUpdate", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "products.bulk.update") || context.User.HasClaim("permission", "catalog.manage")));
            options.AddPolicy("ProductsHistoryRead", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "products.history.read") || context.User.HasClaim("permission", "catalog.manage")));
            options.AddPolicy("ProductsHistoryRestore", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "products.history.restore") || context.User.HasClaim("permission", "catalog.manage")));
            options.AddPolicy("ProductsQualityRead", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "products.quality.read") || context.User.HasClaim("permission", "catalog.manage")));
            options.AddPolicy("ProductsQualityRecalculate", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "products.quality.recalculate") || context.User.HasClaim("permission", "catalog.manage")));
            options.AddPolicy("CategoriesReorder", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "categories.reorder") || context.User.HasClaim("permission", "catalog.manage")));
            options.AddPolicy("ReferencesRead", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "references.read") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("ReferencesManage", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "references.create") || context.User.HasClaim("permission", "references.update") || context.User.HasClaim("permission", "references.manage") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("ReferencesPublish", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "references.publish") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("ReferencesArchive", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "references.archive") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("ShowroomRead", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "showroom.read") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("ShowroomManage", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "showroom.manage") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("ShowroomPublish", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "showroom.publish") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("BannersRead", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "banners.read") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("BannersManage", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "banners.manage") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("BannersPublish", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "banners.publish") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("SettingsRead", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "settings.read") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("SettingsManage", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "settings.manage") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("ContentPreview", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "content.preview") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("ContentSchedule", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "content.schedule") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("ContentHistoryRead", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "content.history.read") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy("ContentHistoryRestore", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "content.history.restore") || context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy(
                "MediaManage",
                policy => policy.RequireClaim("permission", "media.manage"));
            options.AddPolicy("MediaRead", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "media.read") || context.User.HasClaim("permission", "media.manage")));
            options.AddPolicy("MediaUpload", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "media.upload") || context.User.HasClaim("permission", "media.manage")));
            options.AddPolicy("MediaRetry", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "media.retry") || context.User.HasClaim("permission", "media.manage")));
            options.AddPolicy("MediaArchive", policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", "media.archive") || context.User.HasClaim("permission", "media.manage")));
            options.AddPolicy(
                "MediaImport",
                policy => policy.RequireAssertion(context =>
                    context.User.HasClaim("permission", "media.import") ||
                    context.User.HasClaim("permission", "products.import")));
            options.AddPolicy(
                "MediaImportRollback",
                policy => policy.RequireAssertion(context =>
                    context.User.HasClaim("permission", "media.import.rollback") ||
                    context.User.HasClaim("permission", "products.import.rollback")));
            options.AddPolicy(
                "UserManage",
                policy => policy.RequireClaim("permission", "users.manage"));
            options.AddPolicy(
                "ContentManage",
                policy => policy.RequireClaim("permission", "content.manage"));
            options.AddPolicy(
                "SeoAccess",
                policy => policy.RequireAssertion(context =>
                    context.User.HasClaim("permission", "catalog.manage") ||
                    context.User.HasClaim("permission", "content.manage")));
            options.AddPolicy(
                "AdminDashboard",
                policy => policy.RequireAssertion(context =>
                    context.User.Claims.Any(claim =>
                        claim.Type == "permission" &&
                        claim.Value is "catalog.manage" or
                            "quotes.read" or
                            "quotes.manage" or
                            "contacts.read" or
                            "contacts.manage" or
                            "imports.manage" or
                            "imports.publish" or
                            "products.import" or
                            "products.import.rollback" or
                            "media.manage" or
                            "media.import" or
                            "media.import.rollback" or
                            "users.manage" or
                            "content.manage")));
        });
    builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionAuthorizationHandler>();
}

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.Services.GetRequiredService<IConfigurationValidationService>()
        .ValidateStartupConfiguration();
}

app.UseResponseCompression();
// app.UseMiddleware<RequestPerformanceMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiSecurityHeadersMiddleware>();
app.UseMiddleware<Ekiphan.Infrastructure.Seo.SeoRedirectMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseRateLimiter();
if (jwtSettings.IsConfigured)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

var livenessOptions = new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.WriteAsync,
};
app.MapHealthChecks("/api/health", livenessOptions);
app.MapHealthChecks("/api/health/live", livenessOptions);
app.MapHealthChecks(
    "/api/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("ready"),
        ResponseWriter = HealthResponseWriter.WriteAsync,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
        },
    });
app.MapPublicMediaEndpoints();
app.MapCatalogEndpoints();
app.MapContentEndpoints(jwtSettings.IsConfigured);
app.MapMenuEndpoints(jwtSettings.IsConfigured);
app.MapHomepageHeroEndpoints(jwtSettings.IsConfigured);
app.MapGalleryEndpoints(jwtSettings.IsConfigured);
app.MapPressReleaseEndpoints(jwtSettings.IsConfigured);
app.MapAdminSeoEndpoints(jwtSettings.IsConfigured);
app.MapAdminAuthenticationEndpoints(jwtSettings);
app.MapAdminSecurityEndpoints(jwtSettings.IsConfigured);
app.MapAdminUserEndpoints(jwtSettings.IsConfigured);
app.MapRolePermissionEndpoints(jwtSettings.IsConfigured);
app.MapAdminDashboardEndpoints(jwtSettings.IsConfigured);
app.MapAdminProductRelationEndpoints(jwtSettings.IsConfigured);
app.MapAdminProductManagementEndpoints(jwtSettings.IsConfigured);
app.MapAdvancedProductEndpoints(jwtSettings.IsConfigured);
app.MapAdminBrandEndpoints(jwtSettings.IsConfigured);
app.MapAdminCategoryEndpoints(jwtSettings.IsConfigured);
app.MapAdminAttributeEndpoints(jwtSettings.IsConfigured);
app.MapAdminVariantEndpoints(jwtSettings.IsConfigured);
app.MapAdminDictionaryEndpoints(jwtSettings.IsConfigured);
app.MapAdminMediaEndpoints(jwtSettings.IsConfigured);
app.MapMediaProcessingEndpoints(jwtSettings.IsConfigured);
app.MapProductMediaImportEndpoints(jwtSettings.IsConfigured);
app.MapCatalogPdfImportEndpoints(jwtSettings.IsConfigured);
app.MapQuoteSubmission(quoteConsentSettings);
app.MapAdminQuoteEndpoints(jwtSettings.IsConfigured);
app.MapContactEndpoints(quoteConsentSettings, jwtSettings.IsConfigured);
app.MapImportUpload(jwtSettings.IsConfigured);
app.MapImportQueries(jwtSettings.IsConfigured);
app.MapImportPublishing(jwtSettings.IsConfigured);
app.MapProductImportEndpoints(jwtSettings.IsConfigured);
app.MapAdvancedCmsEndpoints(jwtSettings.IsConfigured);
app.MapPublicCmsEndpoints();
app.MapPublicSeoEndpoints();
app.MapAdminAdvancedSeoEndpoints();
app.MapControllers();

await app.Services.BootstrapAdminAsync(builder.Configuration);
app.Run();

public partial class Program;
