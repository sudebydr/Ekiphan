# Ana Sayfa Hero Yönetimi

`/admin/content/heroes` ekranı `content.manage` izniyle ana sayfa banner'larını
yönetir. Kayıtlar TR/EN başlık, alt başlık, birincil ve opsiyonel ikincil CTA,
masaüstü/mobil görsel medya kimliği, sıra, yayın durumu ve opsiyonel başlangıç/
bitiş zamanı içerir.

CTA hedefleri yalnız `/` ile başlayan site içi yollar veya `#` anchor olabilir;
protokol-relative ve harici adresler reddedilir. Bitiş zamanı başlangıçtan sonra
olmalıdır. Yayımlama için en az bir çeviri gerekir. Seçilen medya kayıtlarının
aktif görsel olması backend tarafından doğrulanır.

Public `/api/home/{languageCode}/heroes` projeksiyonu yalnız yayımlanmış, istenen
dilde çevirisi bulunan ve geçerli zaman aralığındaki kayıtları döndürür. Medya
URL'leri güvenli public medya çözümleyicisinden üretilir.

`ManagedHomepageHero` migration'ı kaynak kod olarak oluşturulmuştur; gerçek
veritabanına otomatik uygulanmaz.
