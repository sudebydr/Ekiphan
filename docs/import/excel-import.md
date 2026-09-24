# Excel/CSV Ürün Importu

## Amaç

Yaklaşık 20.000 ürünü tekrarlanabilir, denetlenebilir ve production tablolarını
doğrudan riske atmayan bir hat üzerinden aktarmak.

## Uygulama Durumu

`ImportPipeline` migration'ı ile import job, staging satırı ve satır bazlı doğrulama
sorunlarının kalıcı veri modeli uygulanmıştır. Model Excel, CSV, XML ve IdeaSoft API
kaynaklarını ayırır; SHA-256 checksum ile aynı kaynağın kontrolsüz yeniden alınmasını
engeller ve dry-run ile yayın akışlarını ayrı durum geçişleriyle yönetir.

Ham ve normalize edilmiş satır JSON'ları birlikte saklanır. Satır koordinatı
`job + sheet + row` bileşiminde benzersizdir. Error seviyesindeki bulgular satırın
geçerli işaretlenmesini engeller; warning ve info bulguları denetim amacıyla korunur.

CSV ve XLSX okuyucu altyapısı da uygulanmıştır. Okuyucu:

- Yalnızca `.csv` ve `.xlsx` uzantılarını kabul eder.
- Dosya adında path kullanımını reddeder.
- Varsayılan olarak 25 MB dosya, 50.000 veri satırı, 250 sütun ve hücre başına
  32.767 karakter sınırı uygular.
- CSV içinde virgül, noktalı virgül ve tab ayırıcılarını; quoted ve çok satırlı
  hücreleri destekler.
- XLSX sheet ve özgün satır numarasını korur; makro içeren `.xlsm` dosyalarını
  kabul etmez ve formülleri yeniden hesaplamaz.
- Boş veya yinelenen başlıkları reddeder.

İlk ürün normalizasyonu Türkçe başlık alias'larını SKU, ürün adı, marka, malzeme,
kategori ve etiket alanlarına eşler. SKU büyük harfe çevrilir; kategori ve etiket
çoklu değerleri ayrıştırılır; eksik SKU veya ürün adı hata olarak raporlanır.

Dosyadan `ImportJob` staging kayıtlarına yazan uygulama servisi de uygulanmıştır.
Servis dosya byte'larından SHA-256 üretir, mevcut checksum'ı parse işleminden önce
reddeder, satırları ham ve normalize JSON ile aggregate'e ekler ve tüm aggregate'i
tek `SaveChanges` çağrısıyla atomik kaydeder. Okuyucunun güvenli doğrulama hataları
`Failed` job olarak saklanır; eksik zorunlu kolon değerleri ise satır bazlı issue
üreterek job'ı `ValidationFailed` durumuna taşır.

HTTP upload endpoint'i, JWT permission kontrolü, job/issue sorguları ve güvenli CSV
issue raporu uygulanmıştır. Doğrulanmış yeni SKU'ları yayın dışı Product kayıtlarına
aktaran atomik batch yayın servisi de uygulanmıştır. Mevcut SKU'lar üzerine yazılmaz;
satır `Skipped` olur.

Marka ve kategori referans çözümleme de uygulanmıştır:

- Marka yalnızca mevcut benzersiz canonical `Brand.Name` ile eşleşir.
- Kategori yalnızca Türkçe `CategoryTranslation.Name` üzerinden eşleşir.
- Aynı Türkçe ad birden fazla kategoriye karşılık gelirse `AMBIGUOUS_CATEGORY`
  hatası üretilir.
- Bulunamayan değerler `UNKNOWN_BRAND` veya `UNKNOWN_CATEGORY` olur.
- Sistem import sırasında marka veya kategori tahmin etmez ve otomatik oluşturmaz.
- Çözülen kimlikler normalized payload'a eklenir; yayın sırasında BrandId ve çoklu
  ProductCategory ilişkileri oluşturulur.
- İlk kategori otomatik olarak primary seçilmez.

`Malzeme` alanı da kontrollü attribute sistemine bağlanmıştır:

- Aktif `MATERIAL` kodlu attribute yalnızca `Option` veya `MultiOption` tipinde
  kabul edilir.
- Değer yalnızca aktif option'ın Türkçe çeviri adıyla kesin eşleşir.
- Attribute yoksa `MATERIAL_ATTRIBUTE_NOT_CONFIGURED`, option bulunamazsa
  `UNKNOWN_MATERIAL`, aynı ad birden fazla option'a karşılık gelirse
  `AMBIGUOUS_MATERIAL` üretilir.
- Çözülen attribute/option ID'leri normalized payload'a yazılır.
- Yayın sırasında ham malzeme değeri korunarak typed `ProductAttributeValue`
  oluşturulur.
- Material içermeyen importlarda attribute sorgusu yapılmaz.

Etiket sözlük eşlemesi de uygulanmıştır. Etiketler yalnızca aktif tag'in benzersiz
Türkçe adıyla eşleşir; bilinmeyen değer `UNKNOWN_TAG`, belirsiz ad `AMBIGUOUS_TAG`
olur. Çözülen etiketler yayın sırasında sıralı ProductTag ilişkilerine dönüşür.

Diğer attribute'lar ve admin ekranı halen uygulanmamıştır.

## Akış

1. Dosya yükleme ve import job oluşturma
2. Dosya tipi, boyut, başlık ve satır yapısı doğrulama
3. Ham satırları staging alanına yazma
4. SKU ve zorunlu alan doğrulama
5. Birim, malzeme, marka, kategori ve çoklu değer normalizasyonu
6. İlişki SKU'larını mevcut/aynı dosyadaki ürünlerle çözme
7. Hata ve uyarı raporu üretme
8. Dry-run sonucunu yetkili kullanıcıya gösterme
9. Onaylı job'ı transaction/batch yaklaşımıyla yayınlama
10. Sayım mutabakatı ve audit kaydı

## Zorunlu Güvenceler

- Aynı job tekrar çalıştırıldığında kontrolsüz kopya üretmemeli.
- Satır hataları dosya, sheet, satır, sütun ve hata koduyla raporlanmalı.
- Hatalı ilişki metni ürün adı olarak tahmin edilip otomatik bağlanmamalı.
- Silme işlemi importtan örtük olarak çıkarılmamalı.
- Production importundan önce staging/UAT onayı alınmalı.
- Gerçek IdeaSoft erişimi yoksa entegrasyon çalışıyor sayılmamalı.

## Mevcut Örnek Dosya Bulguları

- Tek sheet: `RESTAURANT MALZEMELERİ`
- 20 sütun ve 5 örnek ürün satırı
- Kategori ve etiketler tek hücrede çoklu değer
- Malzeme büyük/küçük harf kullanımı tutarsız
- Ölçüler serbest metin
- Bazı ilişki alanlarında SKU yerine açıklama/ürün adı var
- TR/EN, SEO, görsel, PDF, yayın durumu ve birincil kategori alanları eksik

## Import Seviyeleri

- Error: satır yayınlanamaz; ör. boş/tekrarlı SKU
- Warning: satır yayınlanabilir ama insan kontrolü gerekir
- Info: uygulanan normalizasyon bilgisi

## IdeaSoft Stratejisi

Tercih sırası:

1. Belgelenmiş ve erişimi doğrulanmış REST API
2. Sürümü sabitlenmiş Excel/CSV export
3. XML export
4. Güvenlik ve şema incelemesi sonrası veritabanı dump

Gerçek bir export örneği sağlanmadan mapping tamamlanmış kabul edilmez.
