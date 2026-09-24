# Markalarımız ve İş Ortaklarımız

Public marka deneyimi Next.js server component'leriyle sunulur:

- `/markalar`: yayımlanmış markaların yönetim sırasına göre listesi.
- `/markalar/{slug}`: açıklama, logo, resmî site, PDF kataloglar, ilgili
  kategoriler ve en fazla 24 yayımlanmış ürün.

Gerçek marka verisi veya logo yoksa production içeriği üretilmez. Logo bulunmayan
kartta marka adından tipografik bir kısaltma gösterilir; yayımlanmış marka yoksa
yönetim paneline yönlendiren dürüst bir boş durum kullanılır.

## Güvenlik

Resmî web sitesi yalnız kullanıcı bilgisi içermeyen mutlak HTTPS adresi olabilir
ve yeni sekmede `noopener noreferrer` ile açılır. Logo ve PDF adresleri yalnız
yapılandırılmış HTTPS public medya tabanından, güvenli storage key çözümlemesiyle
üretilir. Arşivlenmiş, yanlış medya tipindeki veya ilgili dilde metadata'sı
bulunmayan dosyalar public yanıtta yer almaz.

## SEO ve erişilebilirlik

Liste ve detay sayfaları canonical metadata üretir. Marka detayları ve
`/markalar` rotası sitemap'e eklenir. Dinamik marka sayfasının Open Graph görseli
yalnız doğrulanmış public logo mevcutsa kullanılır. Kartlar semantik başlık
hiyerarşisi, görünür odak göstergeleri ve anlamlı logo alt metinleri taşır.
