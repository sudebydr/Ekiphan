# Hedef Mimari

## Durum

Bu belge uygulama öncesi önerilen baseline'dır. ADR kayıtları onaylanmadan teknoloji
seçimleri kesinleşmiş sayılmaz.

## Yaklaşım

Başlangıç için modüler monolit önerilir:

```text
Next.js Public Site + Admin
             |
      ASP.NET Core API
             |
  Modular Application Core
             |
 SQL Server / Cache / Adapters
```

## Modüller

- Identity: admin kullanıcıları, roller ve izinler
- Catalog: ürün, kategori, marka, özellik, varyant ve ilişkiler
- Content: sayfalar, banner, showroom ve kurumsal içerik
- Quote: teklif listesi, talep, durum geçmişi ve bildirim
- Media: güvenli yükleme, kullanım bağımlılıkları ve türev görseller
- Import: staging, doğrulama, dönüşüm ve yayınlama
- Search: SQL tabanlı arama ve gelecekteki sağlayıcı adaptörleri
- LocalizationSeo: çeviri, slug, canonical, hreflang ve redirect
- Administration: yönetim kullanım senaryoları ve audit
- Infrastructure: persistence, cache, e-posta, medya ve telemetry adaptörleri

## Bağımlılık Kuralları

- Domain entity'leri doğrudan API response olarak dönmez.
- Controller iş kuralı, arama sorgusu veya import dönüşümü içermez.
- Modüller birbirlerinin tablolarına kontrolsüz erişmez.
- Harici servisler interface arkasında tutulur.
- Faz-2 ERP/CRM kodu Faz-1'e eklenmez; yalnızca adaptör sınırları korunur.

## Güvenlik Baseline'ı

- Admin yetkileri backend policy seviyesinde uygulanır.
- Secret değerleri environment/secret store üzerinden sağlanır.
- Kişisel veriler loglarda maskelenir.
- Dosya yüklemeleri MIME, boyut, uzantı ve güvenli ad kurallarıyla doğrulanır.
- Embed kaynakları allowlist ile sınırlandırılır.
- Rate limiting, güvenlik başlıkları, CSP ve production hata maskeleme uygulanır.

## Performans Baseline'ı

- Tüm liste endpoint'leri pagination kullanır.
- Ürün, çeviri, kategori ve attribute sorguları için hedefli indexler tasarlanır.
- 20.000 ürünlük temsili veriyle load test yapılmadan harici arama motoru eklenmez.
- Cache invalidation içerik değişikliği olaylarıyla ilişkilendirilir.
- Görseller responsive boyutlarda ve modern formatta sunulur.

## Ortamlar

- Development: anonim/test verisi
- Staging: UAT ve entegrasyon doğrulaması
- Production: onaylı gerçek veri

Production üzerinde geliştirme veya doğrulanmamış migration uygulanmaz.
