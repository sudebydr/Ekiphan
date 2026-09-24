# Medya Kütüphanesi ve Atama Yönetimi

Admin ekranı: `/admin/media`

Ekran aktif/arşivlenmiş medya kayıtlarını server-side arama, tür/durum filtresi
ve sayfalama ile listeler. TR/EN başlık, açıklama ve görsel alt metinlerini
günceller; HTTPS harici video oluşturur ve medyayı ürün, marka veya kategoriye
atar. Dosya alanı birden fazla görsel, PDF veya Word belgesini tek seçimde sırayla
yükleyebilir. Backend her dosyayı bağımsız doğrular.

Görseller, orijinal dosya URL'si üzerinde tarayıcı tarafından sınırlandırılmış
boyutta ve `object-fit: contain` ile önizlenir. Ayrı, fiziksel thumbnail türetme
işlemi henüz yoktur; bunun için güvenlik ve lisans değerlendirmesi yapılmış bir
image-processing bileşeni gereklidir.

## Roller

- Ürün: `GalleryImage`, `PdfCatalog`, `Document`, `Video`
- Marka: `Logo`, `PdfCatalog`
- Kategori: `Image`, `Icon`

Backend rol ile dosya tipinin uyumunu doğrular. Bir üründe tek varsayılan galeri
görseli, bir markada tek logo ve bir kategoride rol başına tek medya bulunur.
Yeni atama bu tekil rollerde mevcut kaydı güvenli biçimde değiştirir. Arşivlenmiş
medya yeni hedeflere atanamaz. Kütüphane ürün, marka, kategori, varyant, hero,
galeri ve basın içeriklerindeki kullanımları toplu, server-side sorgularla gösterir.
Kullanımdaki bir medya hem arayüzde engellenir hem de backend tarafından `409`
ile korunur.

## API

- `POST /api/admin/media`
- `GET /api/admin/media-library?search=&assetType=&status=&page=1&pageSize=40`
- `POST /api/admin/media-library/external-video`
- `PUT /api/admin/media-library/assets/{id}`
- `PUT /api/admin/media-library/assignments`
- `DELETE /api/admin/media-library/assignments/{targetType}/{targetId}/{mediaAssetId}/{role}`

Frontend BFF erişim anahtarını tarayıcıya açmaz, yazmalarda same-origin kontrolü
ve JSON/multipart boyut sınırlarını uygular.

Dosya yükleme, gerçek tehdit tarayıcısı yapılandırılmamışsa güvenlik gereği kapalı
kalır. Dosya adı/path, uzantı-MIME-imza uyumu, dosya boyutu, SHA-256 tekrar
kontrolü ve storage rollback uygulanır. Sunucu storage key'i kendisi üretir.
Harici videolar yalnızca mutlak HTTPS adresleriyle oluşturulur. Tüm endpointler
`media.manage` yetkisini backend'de zorunlu tutar; admin menüsü de aynı yetkiyle
görünür.
