# SEO ve İndeksleme Temeli

Public Next.js uygulaması canonical URL, robots politikası ve dinamik sitemap üretimi
için server-only `EKIPHAN_SITE_URL` ayarını kullanır.

## Site origin sözleşmesi

- Production değeri yalnız mutlak `https` origin olabilir.
- Kullanıcı bilgisi, query string ve fragment içeren değerler reddedilir.
- Development ortamında `http://localhost` ve `http://127.0.0.1` kabul edilir.
- Development varsayılanı `http://localhost:3000` değeridir.
- Production ortamında geçerli origin yoksa sayfalar `noindex, nofollow` olur,
  `robots.txt` bütün crawling'i kapatır ve sitemap URL üretmez.

Bu güvenli varsayılan, staging veya yanlış yapılandırılmış bir deployment'ın arama
motorlarında production sitesi gibi görünmesini engeller.

## İndeksleme politikası

- `/` ve `/katalog` canonical metadata ve Open Graph metadata taşır.
- `/katalog/{slug}` canonical URL'yi API'den dönen normalize slug ile üretir.
- Ürün metadata isteği başarısızsa sayfa `noindex, nofollow` olur.
- `/teklif-listem`, `/admin/*` ve `/api/*` indekslenmez.
- Query string ile filtrelenmiş katalog görünümleri `/katalog` canonical URL'sine
  bağlanır.

## Sitemap veri akışı

`/sitemap.xml`, ana sayfa ile katalog rotasını ve
`GET /api/catalog/tr/sitemap` cevabındaki yayımlanmış ürünleri listeler. Backend
yalnız Türkçe çevirisi bulunan yayımlanmış ürünlerin `slug` ve `updatedAt`
alanlarını seçer; ağır ürün kartı veya görsel projection'ı çalıştırmaz.

Tek sitemap protokol sınırı nedeniyle en fazla 49.998 ürün URL'si eklenir; iki
statik URL ile toplam 50.000 sınırı korunur. Daha büyük katalog hacminde sitemap
index ve parçalara ayrılmış sitemap rotaları ayrı bir iş paketi olarak
uygulanmalıdır.

Katalog servisi geçici olarak kullanılamazsa sitemap isteği çökmek yerine yalnız
statik public rotaları döndürür.
