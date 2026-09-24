# Admin Dashboard

Admin ekranı: `/admin`

Dashboard, gerçek veritabanı kayıtlarından üretilen operasyonel özeti gösterir:

- toplam, yayındaki ve yayın dışı ürün sayıları,
- toplam kategori ve medya sayıları,
- galeri görseli bulunmayan ürünler,
- Türkçe veya İngilizce çevirisi bulunmayan ürünler,
- yeni ve işlemdeki teklif sayıları,
- son teklif talepleri ve son güncellenen ürünler,
- son 30 gündeki kurumsal içerik güncellemeleri,
- son içe aktarma işinin durumu ve satır sayaçları.

Yanıt yalnızca kullanıcının permission kapsamındaki bölümleri üretir. Sistem
durumu `apiOperational`, `databaseReachable` ve ölçülen
`databaseLatencyMilliseconds` alanlarıyla döner. Veritabanı erişilemiyorsa
uydurma metrik üretilmez; ilgili bölümler `null`, sağlık sonucu başarısız ve
`warnings` dolu döner.

Analitik altyapısı henüz bulunmadığı için görüntülenme, arama popülerliği veya
uydurma trend verileri gösterilmez.

## Güvenlik

`GET /api/admin/dashboard` geçerli JWT ve tanımlı admin izinlerinden en az birini
gerektirir. JWT yapılandırılmamışsa `503`, oturum yoksa `401`, uygun admin izni
yoksa `403` döner. Yanıtlar `private, no-store` olarak işaretlenir.

Frontend isteği `/api/admin/dashboard` BFF rotasından geçirir. HttpOnly admin
cookie'sindeki token yalnızca sunucu tarafında backend'e iletilir.

Frontend shell ve doğrudan admin route erişimi aynı permission eşlemesini
uygular. Backend policy ve veri kapsamı kontrolü nihai yetkilendirme katmanıdır.
