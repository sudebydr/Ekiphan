# Kurumsal İçerik Yönetimi

Admin ekranı: `/admin/content/pages`

Bu modül sahte kurumsal içerik üretmeden Türkçe ve İngilizce sayfa kayıtlarının
hazırlanmasını sağlar.

## Yaşam Döngüsü

- `Draft`: çalışma taslağı
- `Review`: editoryal inceleme
- `Published`: public API ve sayfada görünür
- `Archived`: yayından kaldırılmış kayıt

İçerik en az bir çeviri olmadan yayınlanamaz. Arşivleme kaydı fiziksel olarak
silmez.

## Çevrilebilir Alanlar

- başlık, slug, özet ve güvenli düz metin gövdesi
- meta title ve meta description
- site-relative veya mutlak HTTPS canonical URL
- `noindex` ve `nofollow`

## Yönetilen ana sayfa ve kurumsal içerikler

Kurumsal sayfa editöründeki “Hazır içerik türü” seçimi aşağıdaki URL slug'larını
uygun alt çizgili içerik kodlarıyla birlikte başlatır; gerçek metin veya sayı seed
edilmez:

- `ana-sayfa`: hero için genel giriş metni (özel hero kaydı yoksa kullanılır)
- `ana-sayfa-sayaclar`: her satır `Değer | Etiket`
- `ana-sayfa-neden-ekiphan`: boş satırla ayrılan kartlar; ilk satır başlık
- `ana-sayfa-hakkimizda`: ana sayfa kurumsal tanıtım bloğu
- `ana-sayfa-teklif`: ilk satır `Buton etiketi | /site-ici-url`
- `hakkimizda`, `misyon-vizyon`, `degerler`, `sertifikalar`, `hizmetler`,
  `referanslar`, `iletisim` ve `showroom`: kurumsal sayfalar

Ana sayfadaki öne çıkan ürünler, ürün yönetiminde TR slug'ı `one-cikan` olan
aktif etikete bağlanmış yayımdaki ürünlerden server-side sorgulanır. Öne çıkan
kategoriler yayımdaki kök kategorilerin yönetim sırasından gelir. Markalar,
hero, galeri ve diğer bloklar da kendi gerçek endpointlerini kullanır. Yönetilen
bir blok yayımlanmamışsa sahte pazarlama metni veya sayaç gösterilmez.

Gövde HTML olarak çalıştırılmaz. Public görünüm metni güvenli React çıktısı
olarak paragraflara ayırır; böylece kontrolsüz script veya HTML kabul edilmez.

## Yetkilendirme

Admin API `content.manage` izni gerektirir. BFF bearer tokenı yalnız HttpOnly
cookie üzerinden iletir, same-origin kontrolü uygular ve gövdeyi 128 KB ile
sınırlar.

Public Türkçe URL `/sayfa/{slug}` biçimindedir. Yalnız `Published` içerik
döndürülür. `noindex` kayıtları sitemap'e eklenmez.

Admin liste ve tekil yeniden yükleme sorguları sıralama/filtrelemeyi çeviri DTO
projeksiyonundan önce uygular. Böylece SQL Server iç içe çeviri koleksiyonlarını
tek sorguda üretir ve istemci tarafı değerlendirme veya N+1 oluşmaz.
