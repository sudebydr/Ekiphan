# Showroom Public Temeli

`/showroom` rotası, yönetilebilir içerik servisindeki yayımlanmış Türkçe
`showroom` slug'ını kullanır. Başlık, özet ve gövde `/admin/content/pages`
ekranından gerçek içerik teslim edildiğinde yönetilebilir. İçerik yokken sistem
örnek fotoğraf, adres veya kurumsal metin üretmez; dürüst bir boş durum gösterir.

Opsiyonel `SHOWROOM_TOUR_URL` yalnız server ortamından okunur. Değer; mutlak
HTTPS, geçerli host ve kullanıcı bilgisi içermeme kurallarını karşılamazsa
görünmez. Tur yeni sekmede `noopener noreferrer` ile açılır.

## Embed sınırı

Bu pakette üçüncü taraf iframe veya ham embed HTML kabul edilmez. Üretim CSP'si
harici frame kaynaklarını açmamaktadır. Onaylı tur sağlayıcısının alan adı,
gizlilik/cookie davranışı ve embed sözleşmesi teslim edildikten sonra allowlist
tabanlı iframe desteği ayrı bir paket olarak eklenmelidir. Bu karar verilmeden
genel amaçlı HTML veya script alanı açılmamalıdır.
