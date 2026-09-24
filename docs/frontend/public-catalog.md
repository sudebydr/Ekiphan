# Public Katalog Arayüzü

Public katalog arayüzü Next.js App Router üzerinde sunucu tarafında render edilir.

## Rotalar

- `/`: kurumsal giriş ve katalog yönlendirmesi
- `/katalog`: ürün listesi, arama, filtreleme, sıralama ve sayfalama
- `/katalog/{slug}`: ürün açıklaması, kategori/etiketler, typed teknik özellikler,
  benzer ve tamamlayıcı ürünler
- `/markalar`: yayımlanmış markalar ve iş ortakları
- `/markalar/{slug}`: marka açıklaması, katalogları, kategorileri ve ürünleri
- `/robots.txt` ve `/sitemap.xml`: güvenli indeksleme politikası ve public URL keşfi

## Veri akışı

Frontend, katalog verisini browser içinden doğrudan backend'e göndermek yerine server
component üzerinden `EKIPHAN_API_BASE_URL` adresinden okur. Yerel geliştirme
uyumluluğu için yalnızca bu değer yoksa `NEXT_PUBLIC_API_BASE_URL` fallback'i
kullanılır. Bearer token veya admin cookie public katalog akışına dahil edilmez.

Canonical URL, robots ve sitemap ayrıntıları `docs/frontend/seo-and-indexing.md`
dosyasında açıklanır.

Tüm public katalog istekleri:

- `cache: no-store` ile güncel yayımlanmış veriyi ister.
- Sekiz saniyelik timeout uygular.
- Backend hata ayrıntısını kullanıcıya doğrudan göstermez.
- Slug değerini URL bileşeni olarak encode eder.
- Servis erişilemiyorsa sayfayı düşürmeden güvenli hata durumu gösterir.

## Filtre ve sayfalama

Arama ile bölüm, kategori, marka, etiket ve sıralama değerleri GET formuyla query
string'e yazılır. Bu sayede sonuç sayfası paylaşılabilir, bookmark edilebilir ve
JavaScript kapalıyken de kullanılabilir. Sayfa bağlantıları mevcut filtreleri korur.
Seçenekler backend'deki `/api/catalog/tr/navigation` endpointinden gelir; kategori
ve marka değerleri frontend koduna sabitlenmez.

Aktif arama ve filtreler sonuç başlığının altında okunur etiketler olarak gösterilir.
Her etiket kendi filtresini kaldırır ve sayfalamayı ilk sayfaya döndürür; “Tümünü
temizle” bağlantısı bütün filtreleri kaldırır. Arama metni istemci ve API katmanında
2–100 karakterle sınırlandırılır.

Kategori seçildiğinde `/api/catalog/tr/facets?category={slug}` üzerinden yalnızca
o kategoriye atanmış ve `IsFilterable` olarak işaretlenmiş dinamik özellikler
yüklenir. Seçenek, çoklu seçenek, metin ve evet/hayır alanları gerçekten kullanılan
değerleri; sayısal alanlar mevcut ürün değerlerinden hesaplanan min–maks aralığını
gösterir. Attribute filtreleri URL'de tekrarlanabilir `attribute={id}:{token}`
parametreleriyle korunur ve en fazla 20 filtre kabul edilir.

## Erişilebilirlik ve responsive davranış

- Tek bir görünür `h1` ve anlamlı landmark/section başlıkları kullanılır.
- Form alanlarının tamamı görünür `label` taşır.
- Sonuç sayfalaması `nav` ve açıklayıcı `aria-label` ile sunulur.
- Servis hataları `role="alert"` ile bildirilir.
- Klavye odak göstergeleri link, buton ve form kontrollerinde korunur.
- Benzer ve tamamlayıcı ürün grupları ayrı, adlandırılmış section'lar olarak sunulur;
  boş gruplar ekranda gösterilmez.
- Kart düzeni geniş ekranda üç, orta ekranda iki, mobilde tek kolona düşer.
- Filtre paneli mobilde sticky davranışı bırakır, açılıp kapanabilir ve dokunmatik
  kontrollere uygun minimum yüksekliği korur.

Aktif ve ilgili dilde alt metni bulunan `MediaAsset` görselleri yapılandırılmış HTTPS
public medya adresinden gösterilir. Liste ve ilişkili ürün kartları varsayılan/ilk
görseli lazy-load eder; detayın ana görseli yüksek öncelikli, kalan galeri görselleri
lazy-load edilir. Görsel veya güvenli public URL yoksa yanıltıcı stok görsel
kullanılmaz; ürün adından oluşturulan tipografik placeholder korunur.
