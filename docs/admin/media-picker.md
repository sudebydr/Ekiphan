# Ortak Admin Medya Seçici

Ana sayfa hero, galeri ve basın odası formları ham medya GUID girişi yerine ortak
medya seçiciyi kullanır.

Seçici:

- server-only admin medya BFF üzerinden mevcut kütüphaneyi okur,
- yalnızca aktif ve alanla uyumlu medya türlerini gösterir,
- dosya adı, Türkçe/İngilizce başlık ve alt metinde arama yapar,
- görselleri önizler; PDF/doküman türünü açıkça belirtir,
- seçimi değiştirme ve kaldırma işlemlerini destekler,
- kullanıcıya storage key veya iç dosya yolu göstermez.

Yeni depolama veya migration gerektirmez.
