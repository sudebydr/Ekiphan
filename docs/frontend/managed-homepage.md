# Yönetilebilir Ana Sayfa

Ana sayfa mevcut güvenli public servislerden veri toplar:

- Hero başlığı ve özeti, yayımlanmış `ana-sayfa` slug'ına sahip içerik
  sayfasından gelir.
- Ürün dünyaları, yayımlanmış katalog bölümlerinden oluşturulur.
- En fazla altı marka/iş ortağı, yönetim sırasına göre public marka
  projeksiyonundan gelir.
- Showroom çağrısı gerçek `/showroom` rotasına yönlendirir.

Servis veya veritabanı geçici olarak kullanılamazsa ana sayfa tamamen çökmez.
Dinamik ürün ve marka bölümleri gösterilmez; mevcut güvenli hero ve ana katalog
yönlendirmesi korunur. Gerçek veri yerine örnek marka, kategori, logo veya
istatistik üretilmez.

Hero görseli/video, zamanlanmış banner ve yönetilebilir CTA kayıtları bu temel
pakete dahil değildir. Bunlar gerçek medya ve içerik iş akışıyla birlikte ayrı
bir kalıcı ana sayfa bileşen modeli olarak eklenmelidir.
