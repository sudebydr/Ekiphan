# Basın Odası Yönetimi

Public ekran `/basin-odasi`, admin ekranı `/admin/content/press` adresindedir.

Her yayın; yayın tarihi, yayın durumu, `tr`/`en` başlık-özet-metin alanları, isteğe
bağlı kapak görseli ve PDF/doküman eki tutar. Medya dosyaları mevcut medya
kütüphanesinden seçilir; tekrar yüklenmez.

- `GET /api/press/{languageCode}`
- `GET|POST /api/admin/content/press-releases`
- `PUT /api/admin/content/press-releases/{id}`

Public uç yalnızca yayımlanmış ve istenen dilde çevirisi bulunan kayıtları tarihe
göre döndürür. Admin uçları `content.manage` izni ister ve JWT ayarı yoksa kapalıdır.

`ManagedPressRoom` migration kaynağı oluşturulmuş, gerçek veritabanına uygulanmamıştır.
