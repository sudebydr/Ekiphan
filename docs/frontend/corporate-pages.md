# Kurumsal Public Sayfalar

Aşağıdaki kullanıcı dostu rotalar mevcut yönetilebilir içerik altyapısına
bağlıdır:

- `/hakkimizda`
- `/hizmetler`
- `/referanslar`
- `/galeri`
- `/basin-odasi`
- `/iletisim`

Her rota aynı slug ile `/admin/content/pages` ekranından yayımlanan TR içeriği
okur. İçerik yoksa `404`, servis kesintisinde kişisel veya altyapı ayrıntısı
içermeyen güvenli hata durumu gösterilir. Meta title, description, canonical,
noindex ve nofollow alanları içerik kaydından üretilir.

Sitemap bu bilinen slug'ları `/sayfa/{slug}` yerine kullanıcı dostu kök
rotalarıyla yayımlar. Galeri medya düzeni, iletişim formu ve basın dosyaları
ayrı iş paketleridir; gerçek veri teslim edilmeden örnek içerik üretilmez.
