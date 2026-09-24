# İletişim Talepleri

Public form `/iletisim`, admin liste ve detay ekranı `/admin/contact` adresindedir.

Form; ad soyad, e-posta, isteğe bağlı telefon ve şirket, konu, mesaj ve zorunlu
KVKK onayı alır. Görünmez bot alanı, 16 KB istek sınırı, aynı-origin BFF kontrolü
ve public gönderim rate limit'i uygulanır.

KVKK sürümü `QuoteConsent:KvkkVersion` üzerinden yönetilir. Bu değer yoksa form
fail-closed davranır ve kişisel veri kaydetmez. Liste ve detay uçları
`contacts.read` veya `contacts.manage`; yazma uçları `contacts.manage` ister.
JWT yoksa admin uçları kapalı kalır.

- `POST /api/contact`
- `GET /api/admin/contact-requests`
- `GET /api/admin/contact-requests/{id}`
- `GET /api/admin/contact-requests/assignees`
- `POST /api/admin/contact-requests/{id}/status`
- `POST /api/admin/contact-requests/{id}/assignment`
- `POST /api/admin/contact-requests/{id}/notes`

Liste; server-side sayfalama, arama, tarih/durum/atanan filtreleri, yeni kayıt
filtresi ve yeni/eski sıralamasını destekler. E-posta ve telefon listede maskelenir;
tam mesaj ve iletişim bilgileri yalnızca yetkili detay yanıtında döner. Durum
geçmişi, atama ve dahili notlar admin kimliği ve zaman bilgisiyle kaydedilir.

E-posta bildirimi gerçek sağlayıcı bilgileri teslim edilene kadar eklenmemiştir.
Faz 7 şeması `Phase7RequestManagement` migration'ıyla yönetilir.
