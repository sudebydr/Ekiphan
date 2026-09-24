# Kategori ve Ürün Bölümü Yönetimi

Katalog hiyerarşisi `/admin/catalog/categories` sayfasından yönetilir.

## Yetenekler

- Ürün bölümü oluşturma ve güncelleme
- Bir bölüm altında kategori oluşturma ve güncelleme
- Türkçe ve opsiyonel İngilizce ad, slug ve kategori açıklaması
- Yayın durumu ve sıralama yönetimi
- Aynı ürün bölümü içinde en fazla beş seviyeli kategori ağacı
- Ağaç görünümünde seviye ve aktif ürün sayısı
- Medya kütüphanesi üzerinden kategori görseli veya ikonu
- Kendi kendine üst öğe olma, bölümler arası ebeveynlik ve kategori döngüsü engeli
- Çocuk düğümleri derinlik sınırını aşacak taşıma ve alt kategorili düğümün başka
  ürün bölümüne taşınması engeli
- Fiziksel veri silmeden güvenli pasife alma

Referans bütünlüğünü korumak için fiziksel silme yapılmaz. `DELETE` işlemi yalnızca
kategoriyi pasife alır; yayımlanmış alt kategorisi veya aktif ürün ilişkisi bulunan
kategori için `409 Conflict` döner.

## Admin API

- `GET /api/admin/catalog/structure`
- `POST /api/admin/catalog/sections`
- `PUT /api/admin/catalog/sections/{id}`
- `POST /api/admin/catalog/categories`
- `PUT /api/admin/catalog/categories/{id}`
- `DELETE /api/admin/catalog/categories/{id}` (güvenli pasife alma)

JWT yapılandırıldığında tüm yollar `catalog.manage` yetkisi ister; JWT yoksa
kapalı davranır. Cevaplar `private, no-store` olarak işaretlenir. Yazma yolları
katalog yönetimi rate limitini ve 24 KB JSON gövde sınırını kullanır.

Frontend BFF yalnızca açıkça izin verilen bu yol biçimlerini geçirir. Güvenli
admin cookie'sini kullanır, yazma isteklerinde same-origin kontrolü yapar ve
erişim anahtarını tarayıcı JavaScript'ine açmaz.

## Ürün entegrasyonu

Ürün editörü aynı katalog yapısını yükler, bir ürüne birden fazla kategori
atanmasını ve bunlardan birinin ana kategori seçilmesini destekler. Ana kategori
seçili kategori kümesinde bulunmalıdır. Backend kaydetmeden önce bütün kategori
kimliklerini doğrular.

`GET /api/admin/catalog/structure` içindeki her kategori DTO'su `productCount`
alanını taşır. Sayı, silinmemiş ürünlerin kategori atamalarından SQL Server üzerinde
correlated aggregate ile hesaplanır; kategori başına ek sorgu çalıştırılmaz.

Bu paket mevcut tabloları kullanır; yeni migration gerekmez.
