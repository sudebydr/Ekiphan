namespace Ekiphan.Api.Controllers.Admin;

using System.Threading;
using System.Threading.Tasks;
using Ekiphan.Application.Caching;
using Ekiphan.Application.Warmup;
using Ekiphan.Application.Warmup.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/admin/system/[controller]")]
[Authorize] // Would ideally be [Authorize(Policy = "system.performance.read")]
public class PerformanceController : ControllerBase
{
    private readonly ICacheInvalidationService _invalidationService;
    private readonly ICacheWarmupService _warmupService;

    public PerformanceController(
        ICacheInvalidationService invalidationService,
        ICacheWarmupService warmupService)
    {
        _invalidationService = invalidationService;
        _warmupService = warmupService;
    }

    [HttpGet("cache/statistics")]
    public IActionResult GetCacheStatistics()
    {
        // Dummy statistics return
        return Ok(new
        {
            Hits = 1000,
            Misses = 250,
            HitRatio = 0.8,
            TotalKeys = 500
        });
    }

    [HttpPost("cache/invalidate")]
    // [Authorize(Policy = "system.cache.manage")]
    public async Task<IActionResult> InvalidateCache([FromBody] InvalidateCacheRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("Reason is required for audit logs.");

        await _invalidationService.InvalidateByTagsAsync(request.Tags, request.Reason, cancellationToken);
        return Ok();
    }

    [HttpPost("cache/warmup")]
    // [Authorize(Policy = "system.cache.manage")]
    public async Task<IActionResult> TriggerWarmup([FromBody] CacheWarmupRequest request, CancellationToken cancellationToken)
    {
        await _warmupService.WarmupAsync(request, cancellationToken);
        return Accepted();
    }
}

public class InvalidateCacheRequest
{
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Reason { get; set; } = default!;
}
