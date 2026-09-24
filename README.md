# Ekiphan Otel Ekipmanları

Ekiphan için kurumsal web sitesi, dijital ürün kataloğu ve yönetim paneli projesidir.

## Durum

Temel .NET 8 API, Next.js frontend, test ve CI iskeleti oluşturulmuştur. Catalog
çekirdeği, dinamik özellikler, varyantlar, ürün ilişkileri ve medya kütüphanesi
ile teklif yönetimi, denetlenebilir veri aktarımı ve çok dilli etiket altyapısı için
on beş EF Core migration hazırdır. Public teklif endpointi katalogdan güvenli ürün
snapshot'ı üretir ve KVKK/ticari iletişim izin sürümlerini kaydeder;
migration'lar henüz herhangi bir veritabanına uygulanmamıştır.
Yayımlanmış ürünler için çok dilli, sayfalı public katalog listeleme ve detay
endpointleri; bölüm, kategori, marka, etiket ve metin filtreleriyle hazırdır.

## Kapsam Özeti

Faz-1 hedefi:

- Türkçe ve İngilizce kurumsal web sitesi
- Ürünlerimiz ve Endüstriyel Mutfak olarak ayrılmış dijital katalog
- Çok seviyeli kategoriler ve kategoriye özel dinamik filtreler
- Arama, ürün detay, benzer ve tamamlayıcı ürünler
- Çoklu ürün teklif listesi ve teklif talebi yönetimi
- İçerik, katalog, medya, SEO ve teklif yönetimi için admin paneli
- Excel/CSV tabanlı ürün aktarımı
- SEO, erişilebilirlik, güvenlik, cache ve CDN altyapısı

Ürün karşılaştırma son müşteri talebiyle kapsam değişikliği adayıdır. Proje yöneticisi
onayı verilene kadar feature flag varsayılan olarak kapalı tasarlanacaktır.

## Kaynak Önceliği

1. Son tarihli açık müşteri talebi
2. Onaylanmış gereksinim veya kapsam değişikliği
3. FSD/BRD/SAD
4. Onaylanmış teknik karar kayıtları
5. Çalışan uygulama
6. Teknik öneriler ve açık varsayımlar

## Önerilen Teknoloji Yönü

Kabul edilen başlangıç teknoloji yönü:

- Backend: .NET 8 ve ASP.NET Core Web API
- Frontend: Next.js, React ve TypeScript
- Veritabanı: SQL Server
- Mimari: modüler monolit
- Cache: Redis abstraction ve kontrollü fallback
- Arama: Faz-1 için optimize SQL; sağlayıcıdan bağımsız arama arayüzü

Kararlar `docs/decisions/` altındaki ADR kayıtlarıyla kesinleştirilecektir.

## Yerel Gereksinimler

- .NET 8 SDK (bu çalışma ortamında .NET 9 SDK, net8.0 targeting pack ile kullanıldı)
- Node.js 24
- pnpm 11.9

## Çalıştırma

Backend:

```powershell
dotnet restore Ekiphan.sln
dotnet run --project src/backend/Ekiphan.Api/Ekiphan.Api.csproj
```

Frontend:

```powershell
cd src/frontend/ekiphan-web
pnpm install
pnpm dev
```

Sağlık kontrolleri: liveness için `/api/health/live`, veritabanı readiness için
`/api/health/ready`

EF Core araçlarını yüklemek ve migration durumunu kontrol etmek:

```powershell
dotnet tool restore
dotnet ef migrations has-pending-model-changes `
  --project src/backend/Ekiphan.Infrastructure/Ekiphan.Infrastructure.csproj `
  --startup-project src/backend/Ekiphan.Api/Ekiphan.Api.csproj `
  --context EkiphanDbContext
```

## Dokümantasyon

- `docs/requirements-change.md`: kapsam değişiklikleri
- `docs/architecture/overview.md`: hedef mimari
- `docs/database/data-dictionary.md`: veri sözlüğü
- `docs/import/excel-import.md`: veri aktarım kuralları
- `docs/admin/import-management.md`: admin içe aktarma ekranı ve BFF güvenlik sınırı
- `docs/admin/quote-management.md`: admin teklif ekranı, PII ve concurrency sınırı
- `docs/admin/product-relations.md`: manuel ürün ilişkileri admin ekranı ve BFF sınırı
- `docs/admin/product-management.md`: temel ürün CRUD API'si ve soft-delete sözleşmesi
- `docs/admin/dashboard.md`: gerçek operasyonel sayaçları kullanan admin özeti
- `docs/admin/authentication.md`: admin giriş, bootstrap ve token güvenliği
- `docs/admin/user-management.md`: yönetici hesapları ve izin yönetimi
- `docs/admin/content-management.md`: kurumsal sayfa yaşam döngüsü ve SEO yönetimi
- `docs/admin/menu-management.md`: header/footer hiyerarşisi ve güvenli bağlantılar
- `docs/admin/homepage-hero-management.md`: hero medya, CTA ve yayın planlama kuralları
- `docs/admin/gallery-management.md`: yönetilebilir galeri ve public görsel sunumu
- `docs/admin/press-room-management.md`: basın yayınları ve indirilebilir dosyalar
- `docs/admin/contact-request-management.md`: güvenli iletişim formu ve admin talepleri
- `docs/database/managed-navigation.md`: menü tabloları ve indeksleri
- `docs/admin/brand-management.md`: marka yaşam döngüsü ve çok dilli yazma sözleşmesi
- `docs/admin/media-upload.md`: güvenli medya kabul hattı ve fail-closed koşulları
- `docs/admin/media-picker.md`: hero, galeri ve basın odası için ortak medya seçici
- `docs/frontend/public-catalog.md`: public katalog sayfaları ve veri akışı
- `docs/frontend/brands-and-partners.md`: public marka/iş ortağı sayfaları ve güvenlik
- `docs/frontend/showroom.md`: yönetilebilir showroom içeriği ve güvenli tur sınırı
- `docs/frontend/managed-homepage.md`: ana sayfanın yönetilebilir public veri akışı
- `docs/frontend/corporate-pages.md`: kurumsal içeriklerin kullanıcı dostu public rotaları
- `docs/frontend/references-page.md`: onaylı marka ve iş ortaklarını kullanan referanslar sayfası
- `docs/frontend/seo-and-indexing.md`: canonical URL, robots ve sitemap sözleşmesi
- `docs/frontend/quote-request.md`: cihaz-local teklif listesi ve güvenli gönderim
- `docs/api/endpoints.md`: uygulanmış HTTP endpoint sözleşmeleri
- `docs/operations/http-hardening.md`: correlation, sağlık kontrolü ve güvenlik başlıkları
- `docs/decisions/`: mimari karar kayıtları

## Geliştirme Öncesi Kapılar

Kodlama başlamadan önce aşağıdakiler karara bağlanmalıdır:

- Kapsam değişikliği adaylarının Faz-1/Faz-2 durumu
- Hedef hosting ve veritabanı işletim modeli
- IdeaSoft export/API örneği
- Kategori, filtre, birim ve kontrollü değer sözlükleri
- KVKK saklama ve silme politikası
- Kurumsal kimlik ve içerik teslim planı

Gerçek secret değerleri repository içinde tutulmayacaktır.
