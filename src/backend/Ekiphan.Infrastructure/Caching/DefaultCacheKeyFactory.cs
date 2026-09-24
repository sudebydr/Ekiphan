namespace Ekiphan.Infrastructure.Caching;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ekiphan.Application.Caching;
using Ekiphan.Application.Caching.Models;

public class DefaultCacheKeyFactory : ICacheKeyFactory
{
    private const string Prefix = "ekiphan";
    private readonly string _environment = "prod"; // In a real scenario, inject IWebHostEnvironment or similar

    public string ProductDetail(Guid productId, string languageCode)
    {
        return $"{Prefix}:{_environment}:products:v1:detail:{languageCode}:{productId}";
    }
    
    public string ProductDetail(string slug, string languageCode)
    {
        return $"{Prefix}:{_environment}:products:v1:detail:{languageCode}:{slug}";
    }

    public string ProductList(ProductListCacheKeyRequest request)
    {
        var hash = GenerateHash(request);
        return $"{Prefix}:{_environment}:products:v1:list:{request.LanguageCode}:{hash}";
    }

    public string CategoryTree(string languageCode)
    {
        return $"{Prefix}:{_environment}:categories:v1:tree:{languageCode}";
    }

    public string BrandList(string languageCode)
    {
        return $"{Prefix}:{_environment}:brands:v1:list:{languageCode}";
    }

    public string PublicSettings(string languageCode)
    {
        return $"{Prefix}:{_environment}:settings:v1:public:{languageCode}";
    }

    public string CmsEntity(string entityType, Guid entityId, string languageCode)
    {
        return $"{Prefix}:{_environment}:cms:v1:{entityType}:{languageCode}:{entityId}";
    }

    public string Sitemap(string section, string languageCode, int page)
    {
        return $"{Prefix}:{_environment}:sitemap:v1:{section}:{languageCode}:{page}";
    }

    private static string GenerateHash(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
