# Gereksinim ve Kapsam Değişiklikleri

## Yönetim Kuralı

Bu kayıt FSD/BRD/SAD v1.0 sonrasında iletilen müşteri taleplerini izler. Bir kaydın
burada bulunması production kapsamına otomatik olarak alındığı anlamına gelmez.

Durumlar: `Proposed`, `Impact Assessed`, `Approved`, `Rejected`, `Delivered`.

## CR-001 - Ürün Karşılaştırma

- Kaynak: FSD/BRD/SAD sonrası müşteri geri bildirimi
- Durum: Impact Assessed
- Öncelik: Proje yöneticisi onayı bekliyor
- Değişiklik: Ziyaretçi en fazla dört ürünü karşılaştırabilir.
- Etkilenen alanlar: katalog API, dinamik özellikler, ürün kartı/detayı, mobil UI,
  erişilebilirlik, test ve analytics
- Tahmini ek efor: 5-8 adam/gün
- Karar: `ProductComparison` feature flag arkasında geliştirilmeli; varsayılan kapalı
- Production koşulu: yazılı kapsam ve proje yöneticisi onayı

## CR-002 - Showroom

- Durum: Impact Assessed
- Değişiklik: liste/detay, galeri, video ve güvenli harici 360 tur embed desteği
- Tahmini ek efor: 3-6 adam/gün
- Güvenlik: serbest HTML/script kabul edilmez; sağlayıcı allowlist uygulanır
- Açık nokta: içerik ve harici tur sağlayıcısı henüz sağlanmadı

## CR-003 - Markalar ve İş Ortakları

- Durum: Impact Assessed
- Değişiklik: marka liste/detay sayfaları, ilgili ürün/kategori/PDF ve resmî site linki
- Tahmini ek efor: 3-5 adam/gün
- Güvenlik: yalnızca `https` URL; yeni sekmede `noopener noreferrer`

## CR-004 - İki Ürün Dünyası

- Durum: Impact Assessed
- Değişiklik: katalog, `Products` ve `IndustrialKitchen` bölümlerine ayrılır.
- Tahmini ek efor: 2-4 adam/gün
- Veri etkisi: kategori ağacına hard-code edilmemiş `ProductSection` boyutu eklenir.

## CR-005 - Otomatik Benzer Ürün

- Durum: Proposed
- Çelişki: FSD Faz-1'de manuel benzer ürün öngörür; son talep otomatik skoru tarif eder.
- Tahmini ek efor: 4-7 adam/gün
- Öneri: Faz-1'de manuel ilişki; otomatik skor ayrı onay/feature flag ile ele alınsın.
- Uygulama notu: Faz-1 manuel benzer ve tamamlayıcı ilişkilerinin public ürün
  detayında gösterimi teslim edildi; otomatik skor kapsamı hâlâ `Proposed`.

## Çelişkili İş Kuralları

| Konu | FSD/BRD/SAD | Son Talep | Önerilen Baseline |
|---|---|---|---|
| Teklif ürünleri | Tek `ProductId` | Çoklu ürün ve miktar | Çoklu `QuoteRequestItem` |
| Teklif durumu | new/reviewed/replied/closed | Sekiz aşamalı satış akışı | Son talep; ayrıca onay gerekli |
| Benzer ürün | Manuel | Manuel + otomatik | Manuel Faz-1, otomatik öneri |
| Karşılaştırma | Faz-2 olabilir | Ürün sayfasında isteniyor | Feature flag, onay bekliyor |
| Katalog ayrımı | Tek kategori dünyası | İki üst ürün dünyası | `ProductSection` |

## Efor Notu

Kalemlerin örtüşmeleri ayrıştırılmadan toplam ek etki yaklaşık 17-30 adam/gündür.
Bu değer teklif veya takvim taahhüdü değildir; backlog ayrıştırmasından sonra
yeniden tahmin edilmelidir.
