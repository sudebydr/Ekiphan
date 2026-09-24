# Admin Marka Yönetimi

Marka yönetimi backend temeli `catalog.manage` izniyle korunur:

- `GET /api/admin/catalog/brands`
- `GET /api/admin/catalog/brands/{brandId}`
- `POST /api/admin/catalog/brands`
- `PUT /api/admin/catalog/brands/{brandId}`
- `DELETE /api/admin/catalog/brands/{brandId}` (güvenli pasife alma)

Listeleme `page`, `pageSize`, `language=tr|en` ve en fazla 100 karakterlik `search`
parametrelerini kabul eder. Yazma gövdesi marka adı, opsiyonel mutlak HTTPS web
adresi, yönetim sırası, yayın durumu ve benzersiz `tr`/`en` çevirilerini taşır.
Çeviriler açıklama ve slug içerir; PUT tam çeviri kümesi semantiğine sahiptir.

JWT yoksa `503`, oturum yoksa `401`, izin yoksa `403` döner. Yazma gövdesi 16 KB
ile sınırlıdır. Marka adı veya dil bazlı slug çakışması `409` üretir. Ürün veya medya
referanslarının veri bütünlüğünü korumak için fiziksel marka silme yapılmaz.
`DELETE`, markayı yalnızca pasife alır; silinmemiş bir ürüne bağlı marka için
`409 Conflict` döner.

## Admin arayüzü ve ürün bağlantısı

`/admin/catalog/brands` ekranı marka arama, oluşturma, güncelleme, yayın durumu,
sıralama, resmi HTTPS sitesi ve Türkçe/İngilizce açıklama yönetimini sunar.
`/api/admin/catalog/brands*` istekleri server-only katalog BFF allowlist'i üzerinden
iletilir; admin token'ı browser JavaScript'ine açılmaz.

Ürün editörü aynı güvenli marka liste endpointini kullanır ve ürünü mevcut bir
markaya bağlayabilir veya markasız bırakabilir.

Yayımlanmış markalar public `/markalar` ve `/markalar/{slug}` sayfalarında
gösterilir. Logo ile PDF kataloglar medya kütüphanesindeki marka atamalarından,
ürünler ve ilgili kategoriler ise public katalog projeksiyonundan gelir.
Resmî site adresi kullanıcı bilgisi içeremez ve yalnız mutlak HTTPS olabilir.
