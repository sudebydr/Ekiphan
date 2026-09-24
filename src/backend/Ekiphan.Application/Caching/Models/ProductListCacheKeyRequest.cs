namespace Ekiphan.Application.Caching.Models;

using System;
using System.Collections.Generic;

public class ProductListCacheKeyRequest
{
    public string LanguageCode { get; set; } = default!;
    public int Page { get; set; }
    public int PageSize { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public string? Search { get; set; }
    public Dictionary<string, string>? Filters { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public bool PublishedOnly { get; set; }
    public string? Culture { get; set; }
}
