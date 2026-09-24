namespace Ekiphan.Application.Caching;

using System;
using Ekiphan.Application.Caching.Models;

public interface ICacheKeyFactory
{
    string ProductDetail(Guid productId, string languageCode);
    string ProductDetail(string slug, string languageCode);
    string ProductList(ProductListCacheKeyRequest request);
    string CategoryTree(string languageCode);
    string BrandList(string languageCode);
    string PublicSettings(string languageCode);
    string CmsEntity(string entityType, Guid entityId, string languageCode);
    string Sitemap(string section, string languageCode, int page);
}
