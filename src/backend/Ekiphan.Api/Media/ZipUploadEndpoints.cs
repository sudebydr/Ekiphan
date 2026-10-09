using System.Security.Claims;
using System.Threading.Channels;
using Ekiphan.Application.CatalogPdfImport;
using Ekiphan.Application.Media;
using Ekiphan.Domain.Media;
using Microsoft.AspNetCore.Authorization;
using Ekiphan.Application.MediaImport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ekiphan.Api.Media;

public sealed record ZipUploadCreate(string Kind, string FileName, long Length, string Fingerprint);
public sealed record ZipUploadProcess(string Operation, bool Confirmed = false, Dictionary<string, string>? Titles = null, string? ValidationToken = null, Dictionary<string, string>? Fields = null);
internal sealed record ZipUploadWork(ZipUploadState State, Dictionary<string, string>? Titles, string? ValidationToken);

public sealed class ZipUploadWorker(ZipUploadStore store, IServiceScopeFactory scopes, ILogger<ZipUploadWorker> logger)
    : BackgroundService
{
    private readonly Channel<ZipUploadWork> queue = Channel.CreateUnbounded<ZipUploadWork>(new UnboundedChannelOptions { SingleReader = true });
    private readonly HashSet<string> scheduled = [];
    private readonly SemaphoreSlim initialization = new(1, 1);
    private FileStream? processLease;

    public async Task PrepareAsync(CancellationToken ct)
    {
        await initialization.WaitAsync(ct);
        try
        {
            if (processLease is not null) return;
            var lease = store.AcquireProcessLease();
            try { await store.MaintainAsync(restarting: true, ct); processLease = lease; }
            catch { lease.Dispose(); throw; }
        }
        finally { initialization.Release(); }
    }

    public override void Dispose()
    {
        base.Dispose();
        processLease?.Dispose();
        initialization.Dispose();
    }

    internal void Enqueue(ZipUploadState state, Dictionary<string, string>? titles, string? validationToken)
    {
        lock (scheduled)
        {
            var key = $"{state.Id}:{state.Operation}";
            if (scheduled.Contains(key)) return;
            if (!queue.Writer.TryWrite(new(state, titles, validationToken))) throw new InvalidOperationException("İşlem kuyruğu dolu.");
            scheduled.Add(key);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maintenance = CleanAsync(stoppingToken);
        try
        {
            await foreach (var work in queue.Reader.ReadAllAsync(stoppingToken))
            {
                var state = work.State;
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    await using var stream = new FileStream(store.ArchivePath(state.Id), FileMode.Open,
                        FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    object result;
                    if (state.Kind == "image")
                    {
                        result = await SingleProductImageImport.ExecuteAsync(scope.ServiceProvider, stream, state.FileName, state.Owner, stoppingToken);
                    }
                    else if (state.Kind == "pdf")
                    {
                        var fields = state.Fields ?? new Dictionary<string, string>();
                        var title = Field(fields, "title", 250, false);
                        var replaceId = Field(fields, "replaceId", 36, false);
                        var service = scope.ServiceProvider.GetRequiredService<ICatalogPdfImportService>();
                        result = string.IsNullOrEmpty(replaceId)
                            ? await service.UploadSingleAsync(stream, state.FileName, state.Length, title, stoppingToken)
                            : Guid.TryParse(replaceId, out var target)
                                ? await service.ReplaceAsync(target, stream, state.FileName, state.Length, title, stoppingToken)
                                : throw new ArgumentException("Geçerli bir katalog kimliği gerekli.");
                    }
                    else if (state.Kind == "catalog")
                    {
                        var service = scope.ServiceProvider.GetRequiredService<ICatalogPdfImportService>();
                        result = state.Operation == "preview"
                            ? await service.PreviewAsync(stream, state.FileName, state.Length, stoppingToken)
                            : await service.ExecuteAsync(stream, state.FileName, state.Length, work.Titles, stoppingToken);
                    }
                    else
                    {
                        var session = new ZipProductMediaSession(store, scope.ServiceProvider);
                        if (state.Operation == "execute") result = await session.ExecuteAsync(state, stream, stoppingToken);
                        else if (state.Operation == "validate") result = await session.ValidateAsync(state, stoppingToken);
                        else {
                            var preview = await scope.ServiceProvider.GetRequiredService<IProductMediaImportService>().PreviewAsync(
                                new(stream, state.FileName, Path.GetExtension(state.FileName).Equals(".rar", StringComparison.OrdinalIgnoreCase) ? "application/vnd.rar" : "application/zip", state.Length, state.Owner), stoppingToken);
                            await session.CapturePreviewAsync(state, preview, stoppingToken);
                            result = preview;
                        }
                    }
                    await stream.DisposeAsync();
                    await store.CompleteAsync(state.Id, state.Owner, result, null, stoppingToken);
                }
                catch (Exception ex)
                {
                    LogFailure(logger, state.Id, ex);
                    // Never retry persistence automatically after an unknown outcome.
                    var message = ex is ArgumentException or ProductMediaImportSecurityException or UnsafeMediaFileException or MediaUploadUnavailableException or DuplicateMediaContentException
                        ? ex.Message : "ZIP işlenemedi. Sunucu günlüğünü ve import geçmişini kontrol edin; işlem otomatik tekrarlanmadı.";
                    await store.CompleteAsync(state.Id, state.Owner, null, message, CancellationToken.None);
                }
                // Retain this process-lifetime idempotency key: a delayed duplicate HTTP
                // request must not enqueue stale "processing" state after completion.
            }
        }
        finally { try { await maintenance; } catch (OperationCanceledException) { } }
    }

    private static string? Field(IReadOnlyDictionary<string, string> fields, string key, int maximum, bool required)
    {
        var value = fields.GetValueOrDefault(key)?.Trim();
        if ((required && string.IsNullOrEmpty(value)) || value?.Length > maximum)
            throw new ArgumentException($"Geçerli {key} alanı gerekli (en fazla {maximum} karakter).");
        return value;
    }

    private async Task CleanAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
        while (await timer.WaitForNextTickAsync(ct))
            if (processLease is not null) await store.MaintainAsync(restarting: false, ct);
    }

    private static readonly Action<ILogger, Guid, Exception?> LogFailure = LoggerMessage.Define<Guid>(
        LogLevel.Error, new EventId(7501, "ZipUploadProcessingFailed"), "ZIP processing failed. UploadId={UploadId}");
}

internal static class ZipUploadEndpoints
{
    private static readonly System.Text.Json.JsonSerializerOptions WebJson = new(System.Text.Json.JsonSerializerDefaults.Web);
    public static void MapZipUploadEndpoints(this IEndpointRouteBuilder endpoints, bool authenticationConfigured)
    {
        if (!authenticationConfigured) return;
        var group = endpoints.MapGroup("/api/admin/zip-uploads").RequireAuthorization();
        group.AddEndpointFilter(async (context, next) =>
        {
            try
            {
                await context.HttpContext.RequestServices.GetRequiredService<ZipUploadWorker>().PrepareAsync(context.HttpContext.RequestAborted);
                var authorization = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
                var command = context.Arguments.OfType<ZipUploadCreate>().FirstOrDefault();
                var id = context.Arguments.OfType<Guid>().FirstOrDefault();
                var kind = command?.Kind;
                if (id != Guid.Empty)
                    kind = (await context.HttpContext.RequestServices.GetRequiredService<ZipUploadStore>()
                        .GetAsync(id, Owner(context.HttpContext), context.HttpContext.RequestAborted)).Kind;
                var canImport = (await authorization.AuthorizeAsync(context.HttpContext.User, "MediaImport")).Succeeded;
                var canManage = (await authorization.AuthorizeAsync(context.HttpContext.User, "MediaManage")).Succeeded;
                if (kind is "image" ? !canManage : kind is not null ? !canImport : !canManage && !canImport)
                    return Results.Forbid();
                context.HttpContext.Items["ZipCanImport"] = canImport;
                context.HttpContext.Items["ZipCanManage"] = canManage;
                return await next(context);
            }
            catch (KeyNotFoundException ex) { return Results.Problem(statusCode: 404, detail: ex.Message); }
            catch (IOException) { return Results.Problem(statusCode: 503, detail: "Geçici yükleme alanı kullanılamıyor veya başka sunucu işlemi tarafından kullanılıyor.",
                extensions: new Dictionary<string, object?> { ["code"] = "UPLOAD_STORAGE_UNAVAILABLE" }); }
            catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 503, detail: "Sunucunun geçici yükleme dizini izinleri kontrol edilmeli.",
                extensions: new Dictionary<string, object?> { ["code"] = "UPLOAD_STORAGE_UNAVAILABLE" }); }
        });
        // Do not apply media-upload (10 requests / 5 minutes) to hundreds of chunks/status reads.
        group.MapGet("/", (HttpContext context, ZipUploadStore store, CancellationToken ct) =>
            Run(async () => Results.Ok((await store.ListAsync(Owner(context), ct)).Where(s => s.Kind == "image"
                ? context.Items["ZipCanManage"] is true : context.Items["ZipCanImport"] is true))));
        group.MapPost("/", (ZipUploadCreate command, HttpContext context, ZipUploadStore store,
            IOptions<ProductMediaImportOptions> media, CancellationToken ct) => Run(async () =>
            Results.Ok(await store.CreateAsync(Owner(context), command.Kind, command.FileName, command.Length,
                command.Fingerprint, command.Kind switch { "catalog" => ZipUploadStore.CatalogMaximumBytes, "image" => 4L * 1024 * 1024,
                    "pdf" => 500L * 1024 * 1024, _ => media.Value.MaxZipBytes }, ct))))
            .WithMetadata(new RequestSizeLimitAttribute(16384));
        group.MapGet("/{id:guid}", (Guid id, HttpContext context, ZipUploadStore store, CancellationToken ct) =>
            Run(async () =>
            {
                var state = await store.GetAsync(id, Owner(context), ct);
                return Results.Ok(context.Request.Query["includeChunks"] == "true" ? state : state with { Chunks = [] });
            }));
        group.MapPut("/{id:guid}", (Guid id, long offset, HttpContext context, ZipUploadStore store, CancellationToken ct) => Run(async () =>
        {
            if (context.Request.ContentLength is not > 0 or > ZipUploadStore.ChunkBytes)
                return Results.Problem(statusCode: 413, detail: "Geçersiz parça boyutu.");
            var state = await store.AppendAsync(id, Owner(context), offset, (int)context.Request.ContentLength.Value,
                context.Request.Headers["X-Chunk-SHA256"].ToString(), context.Request.Body, ct);
            return Results.Ok(new { state.Offset, state.Length });
        })).RequireRateLimiting("zip-chunk").WithMetadata(new RequestSizeLimitAttribute(ZipUploadStore.ChunkBytes));
        group.MapPost("/{id:guid}/process", (Guid id, ZipUploadProcess command, HttpContext context,
            ZipUploadStore store, ZipUploadWorker worker, CancellationToken ct) => Run(async () =>
        {
            if (command.Operation == "execute" && !command.Confirmed)
                return Results.Problem(statusCode: 400, detail: "İçe aktarma onayı gerekli.");
            var current = await store.GetAsync(id, Owner(context), ct);
            if (command.Operation == "preview" && current.Kind == "product" && current.Phase == "completed" && current.Operation == "validate") {
                var preview = await new ZipProductMediaSession(store, context.RequestServices).PreviewAsync(id, ct);
                return Results.Ok(current with { Result = System.Text.Json.JsonSerializer.SerializeToElement(preview, WebJson) });
            }
            var state = await store.BeginAsync(id, Owner(context), command.Operation, ct, command.Fields);
            if (state.Phase == "processing") worker.Enqueue(state, command.Titles, command.ValidationToken);
            return Results.Accepted($"/api/admin/zip-uploads/{id}", state);
        })).WithMetadata(new RequestSizeLimitAttribute(1024 * 1024));
        group.MapDelete("/{id:guid}", (Guid id, HttpContext context, ZipUploadStore store, CancellationToken ct) => Run(async () =>
        { await store.DeleteAsync(id, Owner(context), ct); return Results.NoContent(); }));
    }

    private static Guid Owner(HttpContext context) => Guid.TryParse(context.User.FindFirstValue("sub") ??
        context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var owner) ? owner : Guid.Empty;

    private static async Task<IResult> Run(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (ZipUploadException ex) { return Results.Problem(statusCode: ex.Status, detail: ex.Message,
            extensions: new Dictionary<string, object?> { ["code"] = ex.Code }); }
        catch (KeyNotFoundException ex) { return Results.Problem(statusCode: 404, detail: ex.Message); }
        catch (ArgumentException ex) { return Results.Problem(statusCode: 422, detail: ex.Message); }
        catch (InvalidOperationException ex) { return Results.Problem(statusCode: 409, detail: ex.Message); }
    }
}
