# Admin Ürün Yönetimi

## Kapsam

Admin ürün yönetimi backend temeli, ürün kimliği ve çok dilli temel metinlerinin
oluşturulması, okunması, güncellenmesi, yayın durumunun değiştirilmesi ve soft-delete
edilmesini kapsar. Liste sorgusu 20.000 ürün ölçeği için sunucu tarafında sayfalanır;
filtreleme ve sıralama tüm ürünleri browser belleğine yüklemez.

Ürün editörü ayrıca birden fazla kategori atamasını ve seçili kategorilerden
birinin ana kategori olarak belirlenmesini destekler. Backend bilinmeyen kategori
kimliklerini reddeder ve ana kategorinin kategori listesinde bulunmasını zorunlu
kılar.

Seçilen kategorilere atanmış aktif dinamik özellikler ürün formunda otomatik
gösterilir. Text, Number, Boolean, Option ve MultiOption alanları kendi tiplerinde
kaydedilir. Backend zorunlu alanları, kategori atamasını, seçenek sözlüğünü,
MultiOption tekrarlarını ve sayısal birim boyutunu doğrular. Kategori ve özellik
değerleri ürünle birlikte tek `SaveChanges` işlemi içinde yenilenir.

Ürün formu aktif etiketleri, kategori sırasını ve her dil için SEO alanlarını da
yönetir. Meta title 70, meta description 320 karakterle sınırlıdır. Canonical URL
yalnız mutlak HTTPS adresi kabul eder; `noindex` TR/EN bazında tutulur.

Görsel, kapak görseli, PDF katalog ve belge ilişkileri ortak medya kütüphanesinin
ürün atama sözleşmesini kullanır. Çoklu galeri görseli, tek varsayılan kapak ve
`SortOrder` desteklenir. İki seviyeli varyant ekranında her varyant SKU’suna aktif
bir görsel bağlanabilir. Benzer/tamamlayıcı ürünler ayrı ilişki ekranında aynı
`catalog.manage` izin sınırı içinde yönetilir.

## Endpointler

- `GET /api/admin/catalog/products/{productId}`
- `GET /api/admin/catalog/products?page=1&pageSize=20&search=...`
- `POST /api/admin/catalog/products`
- `PUT /api/admin/catalog/products/{productId}`
- `DELETE /api/admin/catalog/products/{productId}`

Endpointler `permission=catalog.manage` claim'i gerektirir. JWT yapılandırılmamışsa
`503`, kimlik yoksa `401`, izin yoksa `403` döner. Cevaplar `private, no-store`
olarak üretilir; yazma işlemleri kullanıcı/IP bazlı katalog yazma rate limitini ve
32 KB istek sınırını kullanır.

## Yazma sözleşmesi

POST ve PUT gövdesi aşağıdaki alanları taşır:

| Alan | Kural |
|---|---|
| `sku` | Zorunlu, kırpılır, büyük harfe çevrilir, en fazla 100 karakter |
| `brandId` | Opsiyonel; verildiğinde mevcut marka olmalıdır |
| `isPublished` | Ürünün public katalog yayın durumu |
| `translations` | Bir veya iki kayıt; dil kodları benzersiz `tr`/`en` |
| `translations[].name` | Zorunlu, en fazla 250 karakter |
| `translations[].slug` | Zorunlu, en fazla 300 karakter |
| `shortDescription` | Opsiyonel |
| `longDescription` | Opsiyonel |

PUT tam çeviri kümesi semantiğine sahiptir: gönderilmeyen mevcut dil kaldırılır,
gönderilen dil eklenir veya güncellenir. Ürün her zaman en az bir çeviriyi korur.
SKU veya dil bazlı slug çakışması `409` döndürür.

DELETE fiziksel silme yapmaz. Ürünü soft-delete eder ve aynı anda yayından kaldırır.
Silinmiş ürün normal admin/public sorgularından gizlenir; audit ve veri bütünlüğü için
veritabanı satırı korunur.

## Liste sorgusu

`GET /api/admin/catalog/products` şu güvenli query alanlarını kabul eder:

- `page`, `pageSize` (1–100)
- `search` (SKU veya TR/EN ürün adı, en fazla 100 karakter)
- `categoryId`, `brandId`
- `isPublished`
- `missingImage`, `missingEnglish`
- `sortBy`: `name`, `sku`, `updatedAt`, `createdAt`
- `sortDirection`: `asc`, `desc`

Sorgu `AsNoTracking`, ayrı `COUNT`, deterministik sıralama ve `Skip/Take`
projection kullanır. Liste DTO’su marka, ana kategori, eksik içerik bayrakları ve
güncellenme zamanını taşır; entity doğrudan dışarı verilmez.

## Admin arayüzü ve BFF

`/admin/catalog/products` ekranı ürün arama/seçme, yeni ürün oluşturma, mevcut ürünü
düzenleme, Türkçe/İngilizce çeviri yönetimi, yayın durumu ve arşivleme işlemlerini
sunmaktadır. Marka seçimi için referans veri endpointi henüz bulunmadığından ekran
mevcut `brandId` değerini korur; yeni üründe marka atamasını sonraki marka yönetimi
paketine bırakır.

Browser backend'e doğrudan erişmez. `/api/admin/catalog/*` BFF rotası:

- Bearer token'ı yalnız `HttpOnly` admin cookie'sinden okur.
- Ürün detay/oluşturma/güncelleme/silme yollarını kesin allowlist ile sınırlar.
- POST ve PUT için JSON içerik türü, same-origin ve 32 KB gövde sınırı uygular.
- GET, POST, PUT ve DELETE cevaplarını `private, no-store` ile iletir.

Form değişiklikleri kaydedilmeden sayfadan ayrılmaya çalışılırsa browser uyarısı
gösterilir. Liste loading/empty/error durumlarını, kayıt işlemleri success/error
durumlarını ve responsive iki kolon/tek kolon düzenini ayrı olarak sunar.

## Migration'lar

- `AdminProductSeo`: `ProductTranslations` tablosuna nullable SEO alanları ve
  varsayılanı `false` olan `NoIndex` ekler.
- `ProductVariantImage`: `ProductVariants` tablosuna nullable `MediaAssetId` FK
  ekler; mevcut varyant veya medya verisini silmez.
