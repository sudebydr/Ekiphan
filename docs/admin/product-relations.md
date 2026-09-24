# Admin Ürün İlişkileri

## Kapsam

`/admin/catalog/relations` ekranı katalog ürünleri arasındaki manuel ilişkileri
yönetir. Desteklenen türler `Similar`, `Complementary`, `Accessory` ve
`Alternative` değerleridir. Otomatik ilişki üretimi bu paketin kapsamında değildir.

## Yetki ve BFF sınırı

- Backend endpointleri `permission=catalog.manage` claim'i gerektirir.
- JWT yapılandırılmamışsa endpointler `503` ile kapalı kalır.
- Next.js BFF bearer token'ı yalnızca `HttpOnly` admin cookie'sinden okuyup backend'e
  iletir; token browser JavaScript'ine açılmaz.
- BFF yalnız ürün listeleme, ilişki listeleme/oluşturma ve ilişki pasifleştirme
  yollarını allowlist eder.
- POST gövdesi 8 KB ile sınırlıdır. POST ve DELETE için cross-site Origin
  denetimi uygulanır.
- Admin cevapları `private, no-store` ve `Pragma: no-cache` taşır.

## Yönetim davranışı

Admin önce kaynak ürünü ad veya SKU ile arar. İlişki listesi kaynak yönündeki
ilişkilerle birlikte çift yönlü ilişkilerin ters yönden görünümünü de içerir.
Yeni kayıtta hedef ürün, ilişki türü, çift yönlülük ve yönetim sırası seçilir.

Pasifleştirme fiziksel silme yapmaz. Aynı manuel ilişki daha sonra yeniden
oluşturulursa mevcut kayıt yeni ayarlarla etkinleştirilir. Aktif duplicate ilişki,
otomatik kökenli kayıtla çakışma ve aynı ürünün kendisine bağlanması reddedilir.
Aktif çift yönlü ters kayıt aynı ürün çiftini zaten kapsıyorsa ikinci kayıt
oluşturulmaz.

## Operasyon notları

Ürün araması silinmemiş ve seçili dilde çevirisi bulunan ürünlerle sınırlıdır.
Yayımlanma durumu admin seçiminde gösterilir; taslak ürünler de yayın öncesi ilişki
hazırlığı için yönetilebilir. Public katalog ise yalnız yayımlanmış ilişkili ürünleri
gösterir.

## Doğrulama kapsamı

Servis ile testlerin aynı kuralları kullanması için mevcut ilişki çözümleme, seçilebilir
ürün, çift yönlü görünürlük ve ters kayıt çakışması koşulları ortak saf
`AdminProductRelationRules` katmanında tutulur. Testler aktif duplicate reddini,
pasif manuel kaydın yeniden etkinleştirilmesini, otomatik kaydın devralınmamasını,
çift yönlü ters görünümü ve silinmiş/çevirisiz ürünlerin elenmesini doğrular.
