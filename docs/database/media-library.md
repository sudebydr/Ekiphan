# Medya Kütüphanesi

## Kapsam

Medya dosyaları veritabanında binary olarak tutulmaz. Veritabanı güvenli storage
anahtarı, MIME, boyut, checksum, çeviri ve kullanım ilişkilerini saklar.

Desteklenen asset tipleri:

- Image: JPEG, PNG, WebP
- Pdf
- Document: DOC, DOCX
- ExternalVideo: mutlak HTTPS URL

SVG, kontrollü sanitization altyapısı bulunmadığı için bu aşamada kabul edilmez.

## Güvenlik Kuralları

- Orijinal dosya adı path içeremez.
- Storage key göreli olmalı; `..`, ters slash ve güvenli olmayan karakter içeremez.
- MIME ve dosya uzantısı eşleşmelidir.
- Görseller en fazla 15 MB olabilir.
- PDF ve belgeler en fazla 50 MB olabilir.
- SHA-256 checksum 64 karakterlik hex değeridir.
- Harici video URL'si yalnızca HTTPS olabilir.
- Görsel çevirilerinde alt metin zorunludur.

Bu kurallar metadata doğrulamasıdır. Gerçek upload servisinde dosya imzası/magic
byte ve zararlı içerik taraması ayrıca uygulanmalıdır.

## Kullanım İlişkileri

- ProductMedia: galeri, PDF katalog, belge ve video
- CategoryMedia: kategori görseli ve ikon
- BrandMedia: logo ve PDF katalog

Bir medya birden fazla yerde kullanılabilir. Media FK'lerinde `Restrict` kullanıldığı
için kullanımdayken fiziksel kayıt silinemez.

## Varsayılan Görsel

- Yalnızca GalleryImage varsayılan olabilir.
- Ürün başına en fazla bir varsayılan görsel bulunabilir.
- Kural hem domain hem check constraint/filtered unique index ile korunur.

## Arşivleme

Medya fiziksel silme yerine `Archived` durumuna alınır ve `ArchivedAt` kaydedilir.
Storage tarafındaki retention ve fiziksel temizleme ayrı bir operasyon politikasıdır.

## Henüz Uygulanmayanlar

- Antivirüs/malware taraması
- WebP dönüşümü
- Thumbnail ve responsive türevler
- Production object storage yazma adaptörü
- Kullanım raporu ve güvenli temizleme servisi

## Public Medya Teslimi

Katalog sorguları aktif `Image` varlıklarını, `GalleryImage` ilişkilerini ve istenen
dilde zorunlu alt metni birlikte doğrular. Varsayılan görsel önce, diğerleri
`SortOrder` ve medya kimliğiyle deterministik sıralanır. Arşivlenmiş, görsel olmayan,
çevirisiz veya alt metinsiz kayıtlar public cevaba çıkmaz.

`IPublicMediaUrlResolver`, storage key ile gerçek dosya sunum adresi arasındaki
sınırdır. `PublicMedia__BaseUrl` yalnız mutlak HTTPS, query ve fragment içermeyen bir
adres olduğunda etkinleşir. Storage key segmentleri URL-encode edilir; path traversal,
mutlak yol ve güvenli olmayan karakterler çözülmez. Yapılandırma yok veya geçersizse
API URL üretmez ve frontend yanıltıcı stok görsel yerine tipografik placeholder
gösterir.

Public teslim çözümleyicisi storage'a yazmaz; aşağıdaki güvenli kabul hattının
ürettiği storage key'leri CDN/object storage okuma adreslerine dönüştürür.

## Güvenli Upload Kabul Hattı

`POST /api/admin/media`, `media.manage` izniyle multipart dosya kabul sözleşmesini
uygular. JPEG, PNG, WebP, PDF, DOC ve DOCX için MIME, uzantı ve gerçek dosya imzası
birlikte doğrulanır. DOCX paketinde `[Content_Types].xml` ile `word/document.xml`
girdileri zorunludur. Dosya adı path içeremez; görseller 15 MB, PDF/belgeler 50 MB,
toplam multipart istek 50 MB + 64 KB ile sınırlıdır.

Doğrulanan içerik için SHA-256 hesaplanır ve storage key istemciden alınmayıp
`media/YYYY/MM/{random-guid}.{extension}` biçiminde sunucu tarafından üretilir.
Storage yazıldıktan sonra metadata kaydı başarısız olursa dosya rollback ile
silinir. Aynı checksum ikinci kez kabul edilmez.

`IMediaThreatScanner` ile gerçek tehdit tarayıcısı, `IMediaFileStorage` ile object
storage sınırı tanımlıdır. Varsayılan threat scanner bilinçli olarak `Unavailable`
döner; gerçek tarayıcı bağlanmadan upload `503` ile fail-closed kalır.
`MediaStorage__LocalRoot` adaptörü yalnız açıkça yapılandırıldığında çalışır ve
hedef yolun kök dışına çıkmasını engeller. Production'da kalıcı instance diski
yerine object storage adaptörü kullanılmalıdır.

## Migration

`MediaLibrary` migration kaynak kodu oluşturulmuştur. Veritabanına uygulanmamıştır.
