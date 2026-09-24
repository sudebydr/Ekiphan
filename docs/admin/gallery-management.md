# Galeri Yönetimi

Admin ekranı `/admin/content/gallery`, public ekran `/galeri` adresindedir.

Her galeri kaydı mevcut medya kütüphanesindeki aktif bir görsele bağlanır. Kayıt;
sıra, yayın durumu ve `tr`/`en` başlık-açıklama alanlarını tutar. Görsel dosyası
galeriye kopyalanmaz.

- `GET /api/gallery/{languageCode}` yalnızca yayımlanmış, aktif görseli ve istenen
  dilde alt metni bulunan kayıtları döndürür.
- `GET|POST /api/admin/content/gallery`
- `PUT /api/admin/content/gallery/{id}`

Admin uçları `content.manage` izni ister ve JWT yapılandırılmamışsa kapalı kalır.
Public sayfa veri yoksa sahte görsel üretmez; güvenli boş durum gösterir.

`ManagedGallery` migration kaynağı hazırlanmıştır fakat gerçek veritabanına
uygulanmamıştır.
