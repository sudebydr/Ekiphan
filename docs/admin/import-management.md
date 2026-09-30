# İçe Aktarma Yönetimi

Admin içe aktarma ekranı `/admin/imports` adresindedir. Ekran; CSV/XLSX yükleme,
dry-run, job geçmişi, doğrulama sorunları, hata raporu indirme ve doğrulanmış bir
job'ı yayımlama akışlarını tek yerde toplar.

## Güvenlik sınırı

Tarayıcı, backend API'ye veya erişim token'ına doğrudan ulaşmaz. Next.js BFF
katmanı `/api/admin/imports/*` isteklerini backend'e iletir ve yalnızca sunucu
tarafında okunabilen `ekiphan_admin_access_token` adlı `HttpOnly` cookie'yi
`Authorization: Bearer` başlığına dönüştürür.

- Token browser JavaScript'ine, `localStorage` veya `sessionStorage` alanına yazılmaz.
- BFF yalnızca tanımlı import rotalarını ve geçerli GUID job kimliklerini kabul eder.
- Değişiklik yapan isteklerde same-origin kontrolü uygulanır.
- İstek gövdesi backend sınırıyla uyumlu olarak yaklaşık 25 MB ile sınırlandırılır.
- Yanıtlar `Cache-Control: no-store` ile döner.
- Backend adresi yalnızca sunucuda kullanılan `EKIPHAN_API_BASE_URL` değişkeninden okunur.

Kullanıcı `/admin/login` üzerinden e-posta ve şifreyle giriş yapar. Giriş başarılı olduğunda backend tarafından bir JWT üretilir ve bu token `ekiphan_admin_access_token` isimli `HttpOnly` cookie'de saklanır. Next.js BFF katmanı, import rotalarına gelen isteklerde token'ı cookie'den okuyup backend'e `Authorization: Bearer` başlığı olarak aktarır. Backend ise import işlemlerinde `ImportManage`, yayınlama işleminde de `ImportPublish` yetkisinin bulunduğunu doğrular.

## Kullanıcı akışı

1. Yetkili kullanıcı CSV veya XLSX dosyasını seçer.
2. Dry-run seçeneği varsayılan olarak açıktır; dosya doğrulanır ancak katalog değişmez.
3. Job ayrıntısında satır sayıları ve en fazla 100 sorun görüntülenir.
4. Tüm sorunlar formül enjeksiyonuna karşı güvenli CSV raporu olarak indirilebilir.
5. Yalnızca başarılı ve yayımlanmamış job'lar, ikinci bir onay adımından sonra yayımlanabilir.

Oturum yoksa ekran güvenli biçimde `401` durumunu açıklar. API adresi
yapılandırılmamışsa `503` gösterilir; her iki durumda da hassas backend ayrıntıları
istemciye sızdırılmaz.

## Yerel yapılandırma

`.env.example` dosyasını `.env.local` olarak kopyalayın ve backend'in yerel adresini
tanımlayın:

```dotenv
EKIPHAN_API_BASE_URL=https://localhost:7064
```

Gerçek token veya secret değerlerini `.env` dosyalarına ya da repository'ye eklemeyin.
