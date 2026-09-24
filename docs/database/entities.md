# Catalog Entity Model

## Uygulama Durumu

Catalog çekirdeğinin ilk migration kapsamı:

- ProductSection ve ProductSectionTranslation
- Category ve CategoryTranslation
- Brand ve BrandTranslation
- Product ve ProductTranslation
- ProductCategory

Dinamik attribute ve birim altyapısı `DynamicAttributes` migration'ında;
varyant ve ürün ilişkileri `VariantsAndRelations`, medya metadata ve kullanım
ilişkileri `MediaLibrary`, teklif snapshot ve durum yönetimi `QuoteManagement`
migration'ında; import job, staging satırı ve doğrulama sorunları `ImportPipeline`
migration'ında eklenmiştir.

## İlişki Özeti

```text
ProductSection 1 -- N Category
Category       1 -- N Category (parent-child)
Product        N -- N Category (ProductCategory)
Product        N -- 1 Brand
Product        1 -- N ProductTranslation
Category       1 -- N CategoryTranslation
Brand          1 -- N BrandTranslation
```

## İş Kuralları

- SKU zorunlu, normalize edilmiş ve benzersizdir.
- Çeviri dili yalnızca `tr` veya `en` olabilir.
- Her varlıkta aynı dil için yalnızca bir çeviri bulunabilir.
- Slug dil içinde benzersizdir.
- Ürün birden fazla kategoriye atanabilir.
- Ürünün en fazla bir birincil kategorisi olabilir.
- Birincil kategori önce ürüne atanmış olmalıdır.
- Kategori kendisinin parent'ı olamaz.
- Marka resmî sitesi yalnızca mutlak HTTPS URL olabilir.
- Soft delete edilen ürün yayın dışına alınır.

## Veritabanı Güvenceleri

- SKU, marka adı ve section code için unique index
- Translation tablolarında birleşik primary key
- Dil + slug için unique index
- `ProductCategories` üzerinde ürün başına tek bir `IsPrimary = 1` filtered index
- Kategori listeleme için section/parent/sort indexi
- Ürün sıralaması için category/sort indexi
- Silinmiş ürünler için global query filter

## Migration

İlk migration: `InitialCatalog`.

İkinci migration: `DynamicAttributes`.

Üçüncü migration: `VariantsAndRelations`.

Dördüncü migration: `MediaLibrary`.

Beşinci migration: `QuoteManagement`.

Altıncı migration: `ImportPipeline`.

Yedinci migration: `TagsAndQuoteConsentVersions`.

Sekizinci migration: `QuoteConcurrency`.

Migration kaynak kodları oluşturulmuştur ancak herhangi bir development, staging
veya production veritabanına uygulanmamıştır.
