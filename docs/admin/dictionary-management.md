# Etiket ve Birim Yönetimi

Admin ekranı: `/admin/catalog/dictionaries`

Ekran katalogda kullanılan etiketleri, ölçü birimlerini ve ürün-etiket
atamalarını tek yerde yönetir.

## Etiketler

- Kodlar büyük harfe dönüştürülür.
- Türkçe ve İngilizce ad/slug alanları ayrı tutulur.
- Aynı dil bir etikette yalnızca bir kez bulunabilir.
- Pasif etiketler yeni ürünlere atanamaz.
- Bir etiketi pasife almak, mevcut ürün bağlantılarını otomatik silmez. Ürün
  kaydedilirken pasif mevcut bağlantı korunabilir veya kaldırılabilir.

## Ölçü Birimleri

- Kod ve boyut değerleri büyük harfe dönüştürülür.
- Dönüşüm katsayısı sıfırdan büyük olmalıdır.
- Baz birimin dönüşüm katsayısı `1` olmak zorundadır.
- Bir boyutta yalnızca bir baz birim bulunabilir.
- Veri bütünlüğü için mevcut birimin boyutu ve baz birim kimliği sonradan
  değiştirilemez; kod, sembol, katsayı ve aktiflik güncellenebilir.

## Ürün Etiketleri

Ürün için gönderilen etiket listesi tam eşitleme yapar: listeden çıkarılan
bağlantılar silinir, yeni bağlantılar eklenir ve gönderim sırası `SortOrder`
olarak kaydedilir.

## Yetkilendirme ve BFF

Tüm backend uçları `permission=catalog.manage` izni ister. JWT ayarları yoksa
backend güvenli biçimde `503`, oturum veya token yoksa `401` döner.

Frontend, istekleri `/api/admin/catalog/...` BFF rotası üzerinden geçirir.
BFF erişim anahtarını tarayıcıya açmaz; yazma isteklerinde same-origin kontrolü
ve 32 KB JSON gövde sınırı uygular. Sözlük backend uçlarının ek sınırı 24 KB'dir.
