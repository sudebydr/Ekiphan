# Devam edebilir ZIP yüklemeleri

## Yaşam döngüsü ve güvenlik

- Kullanıcı/oturum sayısı sınırı kaldırıldı. Admission, `ZipUploads:ReservedBytesLimit` (varsayılan 32 GiB) ve gerçek boş disk kontrolüne bağlıdır. Eşzamanlı parça isteği limiti kullanıcı başına 2; disk yazımı sıralı, RAM istek başına en çok 4 MiB.
- Yarım yüklemeler için `ZipUploads:IdleUploadHours` varsayılan 168 (7 gün) **son etkinlikten** itibaren işler. Status okuma ve başarılı parça bu süreyi yeniler. Aktif body alımı/işleme temizlenmez. Yalnız metadata bulunan sahipsiz GUID dizinleri 24 saat sonra temizlenebilir.
- Tamamlanan önizleme/doğrulama onay veya açık manuel kapatma bekler; otomatik silinmez. Süresiz saklama bir erişim tokenı değildir: bütün endpointler mevcut MediaImport yetkisini ve oturum sahibini kontrol eder. Disk kotası yeni admission'ı sınırlar; yöneticiler disk kullanımını izlemelidir.
- Başarılı execute ZIP'i serbest bırakır. Kapatma yalnız geçici dosyaları etkiler, DB/medya/katalog kayıtlarını silmez. Küçük kapalı/tamamlanmış işlem makbuzları 7 gün tutulur.
- İşlem başlatma/idempotency state diskte; aynı işlem tekrar başlatılmaz. Aktif işlem sırasında restart bilinmeyen persistence sonucunu otomatik tekrarlamaz; geçmiş kontrolü gerekir.
- Ürün önizleme ve doğrulama descriptor'ları özel ZIP dizininde atomik JSON olarak saklanır. Yeni `validate` işlemi aynı batch'i kullanır; execute kısa ömürlü 60/30 dakikalık tokenları yeniden üretir. Bellek kaybı veya token süresi ZIP'i tekrar yükletmez. Geçici çıkarılmış görseller eksikse doğrulanmış retained ZIP'ten aynı item ID'leriyle geri alınır; hash ve batch sahipliği kontrol edilir. Yeni batch oluşturulmaz.
- Bu sürümden önce oluşturulmuş ve descriptor'ı olmayan eski ürün önizlemeleri otomatik kurtarılamayabilir; süresi dolmuş tokenlardan batch kimliği tahmin edilmez. Bu durumda eski yüklemeyi açıkça kapatıp yeniden önizlemek gerekir.
- JWT/admin login süresi değiştirilmez; kimlik doğrulama sona ererse tekrar giriş gerekir, ZIP/prefix korunur. Süresiz erişim tokenı yoktur.
- `USER_UPLOAD_QUOTA` ve `UPLOAD_SESSION_CAPACITY` kaldırıldı. Disk: `UPLOAD_DISK_QUOTA` / `UPLOAD_DISK_SPACE` (507), storage/lock/izin: `UPLOAD_STORAGE_UNAVAILABLE` (503), aktif kapatma: `UPLOAD_BUSY` (409).

## Aktarım

- Parça 4 MiB (önceki 1 MiB). SHA-256 browser/server ve dayanıklı fsync ACK öncesinde korunur. 1.5 GiB için 1536 yerine 384 PUT; 875 MiB için 875 yerine 219 PUT. Bu istek sayısı hesabıdır, WAN hız ölçümü değildir.
- Resume fingerprint'in 1 MiB baş/son örneklemesi değiştirilmedi; eski localStorage pointer'ları uyumludur. Tüm acknowledged prefix parça hash'leri yeniden doğrulanır; yanlış dosya üzerine yazılamaz.
- Tek sıralı aktarım, en çok 4 kontrollü network/geçici HTTP retry; kayıp ACK aynı offset/hash ile idempotent. Tüm ZIP RAM'e alınmaz veya ikinci kez spool'a kopyalanmaz.
- Transfer yüzde ve server processing ayrı. Önizleme/doğrulama/execute aynı worker üzerinden; işleme browser bağlantısından bağımsızdır.
- ZIP güvenliği, encrypted ZIP, expanded size, path traversal, entry/file/ratio limitleri korunur.
- Sentetik 32 MiB hash+fsync benchmark testi 1/4/8 MiB blokları raporlar. Gerçek 1–1.5 GiB arşiv, yavaş WAN ve production yük testi çalıştırılmadı.

## Deploy sonrası gereken ayarlar (burada uygulanmadı)

- `ZipUploads__Root`: web root dışında özel, kalıcı yerel disk; app pool'a yalnız bu dizinde read/write/delete izni. JSON descriptor'lar public sunulmamalıdır.
- `ZipUploads__ReservedBytesLimit`: fiziksel disk kapasitesine göre ayarlayın; çıkarılmış image temp/PDF işleme alanı için ayrıca boşluk bırakın. Varsayılan guard 512 MiB; gerçek kullanımda daha fazla pay ayırın.
- `ZipUploads__IdleUploadHours`: varsayılan 168. Eski ActiveSessionsPerUser/RetainedSessionsLimit ayarları artık kullanılmaz.
- Tek backend worker / tek spool node ve sticky routing. Exclusive worker.lock ikinci process'i 503 ile engeller.
- IIS/ARR/WAF: GET/POST/PUT/DELETE /api/admin/zip-uploads geçmeli; chunk body limiti **en az 4 MiB, tercihen 8 MiB**. Legacy PDF limitlerini azaltmayın.
- Next dispatcher/XHR 15 dakika/parça. IIS/ARR/ANCM request timeout ve slow-client limitleri buna uygun olmalı; timeout'ları sınırsız kapatmayın. 4 MiB parçanın 15 dakikada aktarılabildiği minimum hız yaklaşık 4.7 KiB/s.
- App pool recycle/idle timeout işleme sırasında kesinti yapmamalı. Kontrollü restart; processing sırasında crash otomatik re-execute edilmez. Exactly-once crash recovery için DB outbox gerekir; şema değişikliği yapılmadı.

## Tekli yükleme ve RAR desteği

- Aynı /api/admin/zip-uploads taşıma protokolü image (4 MiB JPG/JPEG/PNG/WebP) ve pdf (500 MiB) türlerini de destekler. Tek ürün görseli, mevcut toplu SKU resolver/validation/execution akışını kullanarak dosya adından ürüne bağlanır; Product + hash tekrarları atlanır, belirsiz veya eksik SKU reddedilir.
- Tek PDF formu yalnız dosya ister; başlık dosya adından üretilir. Toplu katalog slug/storage-key kimliğiyle mevcut katalog bulunur; aynı hash tekrar eklenmez, değişen PDF mevcut ReplaceAsync üzerinden kapakla güncellenir. Belirsiz katalog eşleşmesi reddedilir. Ek form alanları kullanan API istemcilerinde alanlar işlem başlarken makbuza alınır; tekrar istek bunları değiştiremez. Form değerleri resume fingerprint'ine dahil edilir.
- Görsel oturumları MediaManage, katalog/ürün arşivi ve tek PDF oturumları mevcut MediaImport yetkisini gerektirir. Liste yalnız yetkili türleri ve oturum sahibinin kayıtlarını gösterir.
- ZIP okuyucusu .NET ZipArchive olarak korunur. RAR SharpCompress 0.50.4 ile diskten okunur; ürün/PDF validation ve SKU resolver ortak kalır. Solid RAR sıralı okunur, arşiv dosyası ZIP'e dönüştürülmez veya ikinci kez kopyalanmaz.
- Şifreli, eksik çok parçalı, link içeren ve güvenlik limitlerini aşan RAR reddedilir. Entry adları filesystem extraction path olarak kullanılmaz; gerçek açılan byte sayısı başlıktaki sınırı aşamaz.
- RAR testleri küçük, bellekte oluşturulmuş RAR4 stored/solid fixture'ları kullanır. Gerçek büyük RAR/ZIP veya production verisi işlenmedi.
