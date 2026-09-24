# API Endpointleri

## Admin katalog yapısı

- `GET /api/admin/catalog/structure`
- `POST /api/admin/catalog/sections`
- `PUT /api/admin/catalog/sections/{id}`
- `POST /api/admin/catalog/categories`
- `PUT /api/admin/catalog/categories/{id}`

Bu yollar `catalog.manage` yetkisi ister, JWT yapılandırması yoksa kapalı
davranır ve private, cache'lenmeyen cevaplar üretir.

## Admin dinamik özellikler

- `GET|POST /api/admin/catalog/attributes`
- `PUT /api/admin/catalog/attributes/{id}`
- `POST /api/admin/catalog/attributes/{id}/options`
- `PUT /api/admin/catalog/attribute-options/{id}`
- `PUT /api/admin/catalog/category-attributes`
- `DELETE /api/admin/catalog/category-attributes/{categoryId}/{attributeId}`

## Admin ürün varyantları

- `GET|POST /api/admin/catalog/products/{productId}/variants`
- `PUT /api/admin/catalog/products/{productId}/variants/{variantId}`
- `POST /api/admin/catalog/products/{productId}/variant-groups`
- `PUT /api/admin/catalog/products/{productId}/variant-groups/{groupId}`
- `POST /api/admin/catalog/products/{productId}/variant-groups/{groupId}/options`
- `PUT /api/admin/catalog/products/{productId}/variant-options/{optionId}`

## Admin medya kütüphanesi

- `POST /api/admin/media`
- `GET /api/admin/media-library`
- `POST /api/admin/media-library/external-video`
- `PUT /api/admin/media-library/assets/{id}`
- `PUT /api/admin/media-library/assignments`
- `DELETE /api/admin/media-library/assignments/{targetType}/{targetId}/{mediaAssetId}/{role}`

## Health

`GET /api/health` ve `GET /api/health/live`

Authentication gerektirmeyen liveness kontrolleridir. Uygulama sürecinin HTTP
isteklerine cevap verebildiğini gösterir; veritabanı bağlantısını sınamaz.

`GET /api/health/ready`

Authentication gerektirmeyen readiness kontrolüdür. SQL veritabanına bağlantı
kurulabiliyorsa `200`, kurulamıyorsa `503` döndürür. Bütün sağlık cevapları yalnızca
genel durum, kontrol adı, kontrol durumu ve süreyi içerir; exception, connection
string veya başka altyapı detayı açığa çıkarmaz. Cevaplar `Cache-Control: no-store`
ile üretilir.

Tüm API cevaplarında `X-Correlation-ID` bulunur. İstemcinin gönderdiği değer yalnızca
8–64 karakterlik ASCII harf, rakam, `-` ve `_` içeriyorsa korunur; aksi durumda
sunucu güvenli bir değer üretir. Rate limit aşımı RFC Problem Details biçiminde
`429`, mümkün olduğunda `Retry-After` başlığı ve aynı correlation kimliğiyle döner.

## Public Katalog

Public katalog endpointleri authentication gerektirmez, IP başına dakikada 120
istekle sınırlandırılır ve yalnızca `IsPublished` durumundaki ürünleri döndürür.
İstenen dil çevirisi bulunmayan ürünler başka bir dilden otomatik doldurulmaz.

### Galeri

- `GET /api/gallery/{languageCode}`: yayımlanmış galeri görsellerini istenen
  `tr` veya `en` dilinde döndürür.
- `GET|POST /api/admin/content/gallery`
- `PUT /api/admin/content/gallery/{id}`

Admin galeri uçları `content.manage` izni, admin rate limitleri ve private/no-store
cevapları kullanır. JWT yapılandırması yoksa admin işlemleri `503` ile kapalı kalır.

### Basın Odası

- `GET /api/press/{languageCode}`: yayımlanmış basın kayıtlarını tarihe göre döndürür.
- `GET|POST /api/admin/content/press-releases`
- `PUT /api/admin/content/press-releases/{id}`

Kapak yalnızca aktif görsel, indirilebilir ek yalnızca aktif PDF veya doküman olabilir.
Admin uçları `content.manage` izni ister ve JWT ayarı yoksa `503` döndürür.

### İletişim Talepleri

- `POST /api/contact`: zorunlu KVKK onayıyla iletişim talebi oluşturur.
- `GET /api/admin/contact-requests`: en yeni 500 talebi admin kullanıcısına döndürür.

Public uç 16 KB gövde ve `quote-submit` hız sınırını kullanır. KVKK sürümü eksikse
`503`; admin JWT ayarı eksikse admin uç `503` döndürür.

### Ürün Listesi

`GET /api/catalog/{languageCode}/products`

`GET /api/catalog/{languageCode}/facets?category={slug}` seçili kategorinin
filtrelenebilir dinamik özelliklerini ve yalnızca yayımlanmış ürünlerde kullanılan
değerleri döndürür. Ürün listesi isteği en fazla 20 tekrarlanabilir `attribute`
parametresi kabul eder. Token türleri `o:{optionId}`, `b:{true|false}`, `t:{value}`
ve `n:{minimum}:{maximum}` biçimindedir.

`languageCode` yalnızca `tr` veya `en` olabilir. Desteklenen query alanları:

| Alan | Kural |
|---|---|
| `page` | Opsiyonel, varsayılan 1, en az 1 |
| `pageSize` | Opsiyonel, varsayılan 24, 1–100 |
| `q` | SKU, ad ve kısa açıklamada arama; en fazla 100 karakter |
| `section` | Yayımlanmış ürün bölümü slug'ı |
| `category` | Yayımlanmış kategori slug'ı |
| `brand` | Yayımlanmış marka slug'ı |
| `tag` | Aktif etiket slug'ı |
| `sort` | `Name`, `NameDescending` veya `Newest` |

Filtreler birlikte verildiğinde AND semantiği kullanılır. Sonuç `items`, `page`,
`pageSize` ve `totalCount` alanlarını içerir. Deterministik sıralama için eşit
değerlerde Product GUID ikincil anahtardır.

### Ürün Detayı

`GET /api/catalog/{languageCode}/products/{slug}`

Yalnızca yayımlanmış ve istenen dilde slug'ı bulunan ürünü döndürür. Cevap ürün
metinlerine ek olarak yalnızca yayımlanmış marka/kategorileri, aktif etiketleri ve
aktif, ilgili dilde adı bulunan typed özellik değerlerini içerir. Draft, soft-delete
edilmiş veya başka dildeki kayıtlar açığa çıkarılmaz. `similarProducts` ve
`complementaryProducts`, aktif manuel ilişkilerden en fazla 12'şer ürün döndürür.
Çift yönlü ilişkiler ters yönden de okunur; pasif, yayımlanmamış veya istenen dilde
çevirisi bulunmayan hedefler cevapta yer almaz. Sıralama ilişki `sortOrder` ve
ilişki kimliğiyle deterministiktir. Ürün bulunamazsa `404` döner.

Ürün özetindeki opsiyonel `image`, güvenli public URL ve ilgili dilde alt metni
içerir. Detaydaki `images` galerisi varsayılan görsel, yönetim sırası ve medya
kimliğiyle sıralanır. Arşivlenmiş, görsel olmayan, alt metinsiz veya public medya
adresi çözülemeyen kayıtlar cevapta bulunmaz.

Geçersiz dil, pagination, sıralama veya boş/aşırı uzun filtreler RFC Problem Details
biçiminde `400`; rate limit aşımı `429` döndürür.

### Markalar ve İş Ortakları

- `GET /api/catalog/{languageCode}/brands`
- `GET /api/catalog/{languageCode}/brands/{slug}`

Liste, yalnız yayımlanmış ve istenen dilde çevirisi bulunan markaları yönetim
sırası, ad ve GUID ile deterministik biçimde döndürür; sonuç 500 kayıtla
sınırlıdır. Detay; açıklama, güvenli resmî site, ilgili dilde alt metni bulunan
logo, PDF kataloglar, ilgili yayımlanmış kategoriler ve en fazla 24 yayımlanmış
ürünü içerir. Arşivlenmiş veya public adresi çözülemeyen medya dışarıda bırakılır.
Bulunamayan marka `404`, geçersiz dil veya slug `400` döndürür.

### Katalog Navigasyonu

`GET /api/catalog/{languageCode}/navigation`

Filtre ve navigasyon arayüzleri için yayımlanmış ürün bölümlerini, bu bölümlere ait
yayımlanmış kategorileri, yayımlanmış markaları ve aktif etiketleri istenen dilde
döndürür. Kategori kayıtları `sectionId` ve `parentId` taşıdığı için frontend çok
seviyeli yapıyı sabit değer kullanmadan kurabilir. Her liste yönetim sırası ve adla
deterministik biçimde sıralanır.

### Sitemap Ürün Projeksiyonu

`GET /api/catalog/{languageCode}/sitemap`

Sitemap üretimi için yayımlanmış ve istenen dilde çevirisi bulunan ürünlerin yalnız
`slug` ve `updatedAt` alanlarını döndürür. Ürün kartı, görsel veya detay projection'ı
çalıştırılmaz. Tek sitemap protokol sınırını korumak için sonuç slug sırasıyla en
fazla 49.998 üründür. Endpoint public katalog rate limit politikasını kullanır.

## Admin Ürün Yönetimi

| Endpoint | Davranış |
|---|---|
| `GET /api/admin/catalog/products/{productId}` | Temel ürün ve `tr`/`en` çevirilerini döndürür |
| `POST /api/admin/catalog/products` | Yeni ürün oluşturur |
| `PUT /api/admin/catalog/products/{productId}` | Kimlik, marka, yayın durumu ve tam çeviri kümesini günceller |
| `DELETE /api/admin/catalog/products/{productId}` | Ürünü fiziksel silmeden soft-delete eder |

Tüm endpointler `permission=catalog.manage` gerektirir ve admin cache/rate-limit
sınırlarını kullanır. POST/PUT gövdesi 32 KB ile sınırlıdır. SKU ve slug benzersizlik
çakışmaları `409`; geçersiz marka, dil veya alan değerleri `400`; bulunamayan ürün
`404` döndürür. Ayrıntılı sözleşme `docs/admin/product-management.md` dosyasındadır.

## Admin Marka Yönetimi

`GET/POST /api/admin/catalog/brands` ile
`GET/PUT /api/admin/catalog/brands/{brandId}` endpointleri marka listeleme, detay,
oluşturma ve güncelleme işlemlerini sunar. `catalog.manage`, admin cache/rate-limit
kuralları ve 16 KB yazma sınırı uygulanır. Web sitesi yalnız mutlak HTTPS olabilir.
Detaylar `docs/admin/brand-management.md` dosyasındadır.

## Public Teklif Talebi

`POST /api/quotes`

Authentication gerektirmez. JSON gövdesi:

| Alan | Kural |
|---|---|
| `fullName` | Zorunlu, en fazla 200 karakter |
| `companyName` | Zorunlu, en fazla 200 karakter |
| `phone` | Zorunlu, en fazla 50 karakter |
| `email` | Zorunlu, geçerli e-posta, en fazla 254 karakter |
| `country` | Zorunlu, en fazla 100 karakter |
| `city` | Opsiyonel, en fazla 100 karakter |
| `sector` | Opsiyonel, en fazla 150 karakter |
| `projectName` | Opsiyonel, en fazla 200 karakter |
| `message` | Opsiyonel, en fazla 4.000 karakter |
| `languageCode` | `tr` veya `en` |
| `kvkkConsent` | Zorunlu `true` |
| `commercialCommunicationConsent` | Opsiyonel, KVKK'dan ayrı |
| `website` | Honeypot; gerçek kullanıcıda boş kalmalıdır |
| `items` | 1–50 farklı ürün/varyant satırı |
| `items[].productId` | Yayımlanmış Product GUID |
| `items[].variantId` | Opsiyonel, ürüne ait aktif varyant GUID |
| `items[].quantity` | 1–100.000 |
| `items[].note` | Opsiyonel, en fazla 2.000 karakter |

Ürün adı, SKU, marka, varyant açıklaması ve varsayılan görsel storage key istemciden
alınmaz; server, yayımlanmış katalog kayıtlarından snapshot oluşturur. Böylece
istemci fiyat dışı ürün kimliğini veya teklif geçmişini sahte metinle değiştiremez.

Başarılı kayıt `202 Accepted` ile yalnızca kriptografik rastgele suffix taşıyan
`requestNumber` ve `receivedAt` döndürür. Endpoint IP başına 15 dakikada beş istek
ve 64 KB body ile sınırlıdır.

- `400`: alan, izin, ürün, varyant, miktar, duplicate satır veya honeypot geçersiz.
- `413`: istek gövdesi 64 KB sınırını aşmış.
- `429`: rate limit aşılmış.
- `503`: onaylı KVKK/ticari iletişim metni sürümleri yapılandırılmamış.

Gerekli server-only yapılandırmalar:

- `QuoteConsent__KvkkVersion`
- `QuoteConsent__CommercialCommunicationVersion`

Bu değerler onaylı hukuk metni sürüm kimlikleri olmalıdır; gerçek metin veya secret
değildir. Harici bot doğrulama sağlayıcısı üretim açılışından önce ayrıca
bağlanacaktır.

## Admin Teklif Yönetimi

Tüm endpointler JWT içinde `permission=quotes.manage` claim'i gerektirir.
Authentication yapılandırması yoksa `503`, token yok/geçersizse `401`, izin yoksa
`403` döner. Endpoint handler'larının ürettiği başarılı ve doğrulama/hata
yanıtlarında `Cache-Control: private, no-store` ile `Pragma: no-cache` kullanılır;
authentication middleware yanıtları PII içermez.

## Admin Ürün İlişkileri

Tüm endpointler JWT içinde `permission=catalog.manage` claim'i gerektirir.
Authentication yapılandırması yoksa `503`, token yok/geçersizse `401`, izin yoksa
`403` döner. Yanıtlar `private, no-store` ve `Pragma: no-cache` taşır.

| Method ve yol | Davranış |
|---|---|
| `GET /api/admin/catalog/products` | `page`, `pageSize`, `language` ve en fazla 100 karakterlik `search` ile admin ürün araması |
| `GET /api/admin/catalog/products/{productId}/relations` | Aktif manuel ve etkili çift yönlü ilişkileri listeler |
| `POST /api/admin/catalog/products/{productId}/relations` | Hedef, adlandırılmış ilişki tipi, çift yönlülük ve sıra ile ilişki oluşturur |
| `DELETE /api/admin/catalog/relations/{relationId}` | Manuel ilişkiyi fiziksel silmeden pasifleştirir |

POST gövdesi 8 KB ile, admin okuma/yazmaları kullanıcı veya IP bazlı rate limit ile
sınırlıdır. Numeric enum, aynı kaynak/hedef, aktif duplicate, otomatik köken
çakışması ve mevcut çift yönlü ters ilişki reddedilir.

## Admin Medya Upload

`POST /api/admin/media`, `permission=media.manage` gerektiren multipart upload
endpointidir. Tam olarak bir dosya ile adlandırılmış `assetType`, `languageCode`,
`title` ve görseller için `altText` alır. Dosya imzası, MIME ve uzantı eşleşmezse
`422`; duplicate checksum `409`; storage veya tehdit tarayıcısı hazır değilse `503`
döner. Başarı `201` döndürür. Ayrıntılar `docs/admin/media-upload.md` dosyasındadır.

### Teklif Listesi

`GET /api/admin/quotes?page=1&pageSize=20&status=Reviewing&search=hotel`

- `page`: en az 1
- `pageSize`: 1–100
- `status`: opsiyonel, yalnızca `QuoteStatus` adı; sayısal enum kabul edilmez
- `search`: opsiyonel, en fazla 100 karakter; talep no, ad, firma ve e-postada aranır
- Sıralama: en yeni teklif önce, eşitlikte Quote GUID

Özet; iletişim alanları, durum, ürün sayısı, zamanlar ve Base64 `version` değerini
içerir. Okuma limiti kullanıcı başına dakikada 60 istektir.

### Teklif Detayı

`GET /api/admin/quotes/{quoteId}`

İletişim/proje bilgileri, KVKK ve ticari iletişim izin zamanı+sürümü, snapshot ürün
satırları ve kronolojik durum geçmişini döndürür. Kayıt yoksa `404` döner.

### Durum Değişikliği

`POST /api/admin/quotes/{quoteId}/status`

En fazla 16 KB JSON gövdesi:

```json
{
  "status": 2,
  "expectedVersion": "AQIDBAUGBwg=",
  "note": "İncelemeye alındı."
}
```

`status` hedef `QuoteStatus`, `expectedVersion` detay/listede son okunan 8-byte SQL
rowversion'ın Base64 karşılığıdır; `note` opsiyonel ve en fazla 1.000 karakterdir.
Başarılı yanıtta yeni durum ve yeni `version` döner. Değişikliği yapan adminin GUID
`sub` claim'i durum geçmişine kaydedilir.

- Eski rowversion veya eşzamanlı güncelleme: `409`
- Domain durum makinesinde izin verilmeyen geçiş: `409`
- Geçersiz Base64, status, note veya kullanıcı GUID: `400`/`403`
- Kayıt yok: `404`

Yazma limiti kullanıcı başına dakikada 20 istektir.

## Import Upload

`POST /api/admin/imports`

`multipart/form-data` alanları:

| Alan | Tür | Kural |
|---|---|---|
| `file` | Dosya | Tam olarak bir `.csv` veya `.xlsx`, en fazla 25 MB |
| `isDryRun` | Boolean | Opsiyonel; varsayılan `true` |

İzin: JWT içindeki `permission=imports.manage` claim'i.

JWT yapılandırması eksikse endpoint anonim moda düşmez ve `503 Service Unavailable`
döner. Yapılandırma mevcutsa token olmadan `401`, gerekli permission olmadan `403`
döner.

Dosya ve MIME eşleşmeleri:

- `.csv`: `text/csv` veya `application/csv`
- `.xlsx`: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`

Endpoint kullanıcı veya IP başına dakikada beş istekle sınırlıdır. Limit aşımında
`429` döner. Hatalar RFC Problem Details biçimindedir.

Başlıca cevaplar:

- `200`: staging/doğrulama tamamlandı; job özeti döner.
- `400`: dosya, form veya boolean değeri geçersiz.
- `401`: bearer token yok veya geçersiz.
- `403`: `imports.manage` izni yok.
- `409`: aynı SHA-256 checksum daha önce staging'e alınmış.
- `413`: multipart gövde limiti aşılmış.
- `415`: multipart veya dosya MIME türü desteklenmiyor.
- `422`: job güvenli okuma/doğrulama aşamasında `Failed` olmuş.
- `429`: rate limit aşılmış.
- `503`: JWT authentication henüz yapılandırılmamış.

JWT değerleri yalnızca environment variable veya güvenli secret store üzerinden
verilmelidir:

- `Authentication__Jwt__Issuer`
- `Authentication__Jwt__Audience`
- `Authentication__Jwt__SigningKey` — en az 32 karakter

## Import Sorguları

Tüm sorgular `permission=imports.manage` izni gerektirir ve kullanıcı/IP başına
dakikada 60 istekle sınırlıdır.

### Job Listesi

`GET /api/admin/imports?page=1&pageSize=20&status=Completed`

- `page`: en az 1
- `pageSize`: 1–100
- `status`: opsiyonel `ImportJobStatus` adı veya sayısal enum değeri
- Sıralama: en yeni job önce

Cevap `items`, `page`, `pageSize` ve `totalCount` alanlarını içerir. Sorgu
`AsNoTracking` projection kullanır ve domain entity'lerini doğrudan döndürmez.

### Job Detayı

`GET /api/admin/imports/{jobId}`

Checksum, kaynak, durum, sayaçlar ve yaşam döngüsü zamanlarını döndürür. Job yoksa
`404` döner.

### Issue Listesi

`GET /api/admin/imports/{jobId}/issues?page=1&pageSize=20&severity=Error`

Sheet, özgün satır numarası, SKU, severity, kod, kolon ve güvenli ham değer
bilgisini sayfalı döndürür. `severity` opsiyoneldir. Job yoksa `404` döner.

### Issue CSV Raporu

`GET /api/admin/imports/{jobId}/issues.csv?severity=Error`

Issue kayıtlarını bellekte toplamak yerine EF Core async stream üzerinden doğrudan
HTTP response'a yazar. `severity` filtresi opsiyoneldir. Dosya adı yalnızca job
GUID'sinden üretilir.

CSV güvenlik ve uyumluluk kuralları:

- UTF-8 BOM kullanılır.
- Tüm hücreler çift tırnak içine alınır.
- Hücre içindeki çift tırnaklar iki tırnak olarak escape edilir.
- Newline içeren değerler korunur.
- İlk anlamlı karakteri `=`, `+`, `-`, `@`, tab veya carriage return olan hücreler
  başına apostrof eklenerek spreadsheet formula injection engellenir.
- Veriler `AsNoTracking` ve deterministik sheet/satır/severity/kod sırasıyla
  stream edilir.

## Import Yayınlama

`POST /api/admin/imports/{jobId}/publish`

İzin: JWT içindeki `permission=imports.publish` claim'i. `imports.manage` tek başına
yayınlama yetkisi vermez.

İlk güvenli yayın kapsamı:

- Yalnızca `ReadyToPublish` durumundaki, dry-run olmayan job kabul edilir.
- Yeni SKU için yayın dışı Product ve Türkçe ProductTranslation oluşturulur.
- Slug, normalize ürün adı ve benzersiz Product GUID'sinden üretilir.
- Veritabanında mevcut veya aynı işlemde daha önce eklenen SKU değiştirilmez;
  ilgili import satırı `Skipped` olur.
- Marka yalnızca canonical marka adına, kategori yalnızca benzersiz Türkçe kategori
  adına kesin eşleşmişse bağlanır. Çözümlenmiş malzeme typed attribute değeri ve
  aktif etiket bağlantıları da oluşturulur.
- Ürünler otomatik olarak public yayına alınmaz.
- SKU kontrolleri SQL Server parametre sınırı için 1.000'lik gruplarla yapılır.
- Yazımlar 500'lük batch'lerle, tamamı tek veritabanı transaction'ı içinde yürütülür.

Job yoksa `404`; dry-run, yanlış durum veya tekrar yayınlama girişiminde `409`
döner. Başarı cevabı yayınlanan ve atlanan satır sayılarını içerir.

## Admin Etiket ve Birim Sözlükleri

Tüm uçlar JWT içinde `permission=catalog.manage` izni gerektirir. JWT
yapılandırılmamışsa `503`, token yoksa `401`, izin yoksa `403` döner.

- `GET /api/admin/catalog/dictionaries`: etiketleri, birimleri ve ürün-etiket
  atamalarını döndürür.
- `POST /api/admin/catalog/tags`: etiket oluşturur.
- `PUT /api/admin/catalog/tags/{id}`: etiket kodu, çevirileri ve aktifliğini
  günceller.
- `POST /api/admin/catalog/units`: ölçü birimi oluşturur.
- `PUT /api/admin/catalog/units/{id}`: birimin kod, sembol, katsayı ve
  aktifliğini günceller.
- `PUT /api/admin/catalog/products/{id}/tags`: ürünün etiket listesini tam
  eşitler ve gönderim sırasını saklar.

Yazma uçları kullanıcı/IP başına dakikada 20 istekle, JSON gövdesi 24 KB ile
sınırlıdır. Benzersiz kod, slug veya boyut baz birimi çakışmaları `409`; geçersiz
alanlar `400`; bulunamayan kayıtlar `404` döndürür.

## Admin Dashboard

`GET /api/admin/dashboard`

Toplam/yayındaki/yayın dışı ürün, kategori, medya, son 30 günlük içerik
güncellemesi, eksik görsel ve dil içeriği, yeni/işlemdeki teklif, son kayıtlar,
son import ve ölçülen API/veritabanı sağlık özetini döndürür. Entity yerine
dashboard DTO'ları kullanılır ve yalnızca izin verilen veri bölümleri üretilir.
Geçerli JWT içinde `catalog.manage`,
`quotes.manage`, `imports.manage`, `imports.publish` veya `media.manage`
izinlerinin yanında `users.manage` veya `content.manage` izinlerinden en az biri
gerekir. Yanıt `private, no-store` olarak işaretlenir.
JWT yapılandırması yoksa endpoint `503` ile güvenli biçimde kapalı kalır.

## Admin Kimlik Doğrulama

`POST /api/admin/auth/login`

JSON alanları `email` ve `password` değerleridir. Başarılı cevap kısa ömürlü
access token, sona erme zamanı, görünen ad ve izin listesini döndürür. Frontend
BFF tokenı response gövdesinden çıkarıp yalnızca `HttpOnly` cookie'ye yazar.

- Başarılı giriş: `200`
- Geçersiz e-posta/parola, pasif veya kilitli hesap: `401`
- JWT ayarları eksik: `503`
- Gövde limiti aşımı: `413`
- IP başına 15 dakikada beş deneme sonrası: `429`

`GET /api/admin/auth/session`

Geçerli bearer tokenın yanında veritabanındaki oturum kaydını, aktif kullanıcıyı,
security stamp'i ve 30 dakikalık idle timeout'u doğrular. Başarılı cevap yalnızca
kullanıcı kimliği, e-posta, görünen ad, rol, güncel permission listesi ve güvenli
frontend zamanlayıcısı için idle timeout dakika bilgisini içerir; token veya başka
hassas veri içermez. Geçersiz oturum `401`, admin paneline erişim izni bulunmayan
kullanıcı `403` döner.

`POST /api/admin/auth/logout`

Doğrulanmış oturumu backend'de iptal eder. Frontend BFF, backend çağrısının
sonucundan bağımsız olarak yerel `HttpOnly` cookie'yi temizler. Sonraki bearer
istekleri iptal edilmiş oturum nedeniyle `401` döner.

## Admin Kullanıcı Yönetimi

Tüm uçlar `permission=users.manage` gerektirir:

- `GET /api/admin/users?page=1&pageSize=50&search=`
- `POST /api/admin/users`
- `PUT /api/admin/users/{id}`
- `PUT /api/admin/users/{id}/password`

Liste kullanıcıları e-posta ve kimliğe göre kararlı biçimde sıralar, 1-100
arasında sayfa boyutu ve en fazla 100 karakterlik isteğe bağlı arama kabul eder.
Liste ve kayıt yanıtları `private, no-store` kullanır. Geçersiz alanlar `400`,
benzersiz e-posta veya son aktif kullanıcı yöneticisi kuralı çakışmaları `409`,
bulunamayan kullanıcı `404` döndürür.

## Yönetilebilir İçerik

Public:

- `GET /api/content/{languageCode}/pages/{slug}`
- `GET /api/content/{languageCode}/sitemap`

Yalnız yayınlanmış içerikler public uçlardan döner. Sitemap `noindex` kayıtları
dışarıda bırakır.

Admin uçları `permission=content.manage` gerektirir:

- `GET /api/admin/content/pages`
- `POST /api/admin/content/pages`
- `PUT /api/admin/content/pages/{id}`

Yazma gövdesi 128 KB ile sınırlıdır. Kod veya dil-slug çakışması `409`,
geçersiz yaşam döngüsü, canonical URL ya da alan `400` döndürür.

## Yönetilebilir Menü

Public:

- `GET /api/navigation/{languageCode}/{location}`

`languageCode` yalnız `tr` veya `en`, `location` yalnız ad olarak `Header` veya
`Footer` olabilir; sayısal enum kabul edilmez. Yalnız yayımlanmış ve istenen
dilde etiketi bulunan kayıtlar deterministik sırayla döner. Görünür üst kaydı
olmayan alt kayıtlar dışarıda bırakılır.

Admin uçları `permission=content.manage` gerektirir:

- `GET /api/admin/content/menu-items`
- `POST /api/admin/content/menu-items`
- `PUT /api/admin/content/menu-items/{id}`

Yazma gövdesi 24 KB ile sınırlıdır. Menü kodu çakışması `409`; güvensiz URL,
geçersiz dil/konum, üç seviyeyi aşan veya döngüsel hiyerarşi ve yayın kuralları
`400` döndürür. JWT yapılandırması yoksa admin uçları `503` ile kapalı kalır.

## Yönetilebilir Ana Sayfa Hero

- Public: `GET /api/home/{languageCode}/heroes`
- Admin: `GET|POST /api/admin/content/homepage-heroes`
- Admin: `PUT /api/admin/content/homepage-heroes/{id}`

Admin uçları `content.manage` ister, yazma gövdesi 32 KB ile sınırlıdır ve JWT
yapılandırması yoksa `503` döndürür. Public uç yalnız aktif yayın zamanındaki
TR/EN projeksiyonları verir.
