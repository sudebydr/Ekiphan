# Ürün Veri Sözlüğü

## Uygulama Durumu

ProductSection, Category, Brand, Product, çeviri tabloları ve ProductCategory ilk
catalog migration'ında uygulanmıştır. Attribute, option, unit, category attribute
ve typed product attribute value tabloları ikinci migration'da uygulanmıştır.
Varyant grupları, seçenekleri, kombinasyonları ve ürün ilişkileri üçüncü migration'da
uygulanmıştır. Medya asset, çeviri ve kullanım tabloları dördüncü migration'da
uygulanmıştır. Çoklu ürün teklif snapshot, izin ve durum geçmişi tabloları beşinci
migration'da uygulanmıştır. Import job, staging satırı ve satır bazlı sorun tabloları
altıncı migration'da uygulanmıştır.
Çok dilli etiket, ürün-etiket ilişkileri ve teklif izin sürümü snapshot alanları
yedinci migration'da uygulanmıştır.
Teklif yönetiminde kayıp güncellemeyi önleyen SQL rowversion alanı sekizinci
migration'da uygulanmıştır.

## Modelleme İlkeleri

- Teknik adlar İngilizce, kullanıcı metinleri translation tablolarında tutulur.
- SKU metin türündedir ve normalize edilmiş karşılığı benzersizdir.
- Ham import değeri ile normalize edilmiş değer ayrı izlenir.
- Filtrelenebilir sayısal değer sayı ve birim olarak saklanır.
- Çoklu kategori ilişkisi join tablosuyla, birincil kategori açık alanla yönetilir.
- Silme gerektiren iş kayıtlarında soft delete ve audit kullanılır.

## Çekirdek Varlıklar

| Varlık | Amaç | Kritik alanlar |
|---|---|---|
| Product | Dil bağımsız ürün | SKU, BrandId, PrimaryCategoryId, Status |
| ProductTranslation | Dil içeriği | LanguageCode, Name, Description, Slug, SEO |
| ProductSection | Katalog dünyası | Code, IsPublished, SortOrder |
| Category | Hiyerarşi | ParentId, ProductSectionId, Status |
| ProductCategory | Çoklu kategori | ProductId, CategoryId, SortOrder, IsPrimary |
| Brand | Marka | Name, LogoMediaId, WebsiteUrl |
| Attribute | Teknik özellik | DataType, UnitGroup, IsFilterable |
| CategoryAttribute | Kategori filtresi | CategoryId, AttributeId, display/filter flags |
| ProductAttributeValue | Ürün değeri | ProductId, AttributeId, typed value, UnitId |
| ProductRelation | Ürün ilişkisi | SourceId, TargetId, RelationType, SortOrder |
| ProductVariant | Satılabilir/seçilebilir varyant | SKU, ProductId, Status |
| MediaFile | Medya metadata'sı | StorageKey, MIME, Size, Status |
| QuoteRequest | Teklif başlığı | RequestNumber, contact snapshot, Status |
| QuoteRequestItem | Teklif satırı | product snapshot, Variant, Quantity, Note |
| ImportJob | Dosya/API aktarım çalışması | SourceType, checksum, dry-run, Status, sayaçlar |
| ImportRow | Denetlenebilir staging satırı | SheetName, RowNumber, SKU, ham/normalize JSON, Status |
| ImportIssue | Satır doğrulama bulgusu | Severity, Code, ColumnName, RawValue |
| Tag | Kontrollü ürün etiketi | Code, IsActive, TR/EN translations |
| ProductTag | Ürün-etiket ilişkisi | ProductId, TagId, SortOrder |

## Kontrollü Değerler

İlk kontrollü sözlükler:

- ProductSection: `Products`, `IndustrialKitchen`
- Language: `tr`, `en`
- RelationType: `Similar`, `Complementary`, `Accessory`, `Alternative`
- AttributeDataType: `Text`, `Number`, `Boolean`, `Option`, `MultiOption`
- ContentStatus: `Draft`, `Review`, `Published`, `Archived`

Teklif durum listesi müşteri/proje yöneticisi onayı beklemektedir.

## Excel Eşleştirme Taslağı

| Excel sütunu | Hedef | Dönüşüm |
|---|---|---|
| ürün kodu | Product.SKU | trim + benzersizlik |
| ürün adı | ProductTranslation.Name | başlangıçta `tr` |
| kısa açıklama | ProductTranslation.ShortDescription | başlangıçta `tr` |
| Hacim/Birim | Attribute value | decimal + normalize unit |
| Ölçü/ebat | Ayrıştırılmış ölçüler | parser + review |
| renk | Option | kontrollü sözlük |
| Kullanım amacı | MultiOption | kontrollü sözlük |
| Malzeme | Option | case/alias normalizasyonu |
| marka | Brand | normalize ad + review |
| kategori alanları | ProductCategory | ayırıcı + mapping tablosu |
| etiket | Tag ilişkisi | ayırıcı + trim |
| ilişki kodları | ProductRelation | yalnızca geçerli SKU |
| varyant | ProductVariant | format kararı gerekli |
| sıralama | ProductCategory.SortOrder | integer |

## Açık Veri Kararları

- Ondalık ve binlik ayırıcı
- Ölçü formatı ve boyut sırası
- İzin verilen birimler ve dönüşüm kuralları
- Çoklu değer ayırıcısı
- Kategori mapping sahipliği
- Ürün ilişkilerinin yönü
- Varyant formatı ve varyant SKU kuralı
