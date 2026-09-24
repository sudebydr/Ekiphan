# Admin Kimlik Doğrulama

Admin giriş ekranı: `/admin/login`

Kimlik doğrulama yerel SQL Server kullanıcı deposu, ASP.NET Core parola hasher ve
kısa ömürlü JWT kullanır. Tarayıcı token değerini JavaScript ile okuyamaz.
Next.js BFF başarılı girişten sonra tokenı `HttpOnly`, `SameSite=Lax`,
production ortamında `Secure` cookie olarak saklar.

## Güvenlik Kuralları

- Giriş uç noktası IP başına 15 dakikada 5 istekle sınırlıdır.
- Beş başarısız parola denemesi hesabı 15 dakika kilitler.
- Hatalı kullanıcı ve hatalı parola aynı genel `401` cevabını üretir.
- Erişim tokenı varsayılan 60 dakika geçerlidir; izin verilen aralık 30–480
  dakikadır.
- Her oturum veritabanında ayrı kayıtla izlenir ve son doğrulanmış aktiviteden
  30 dakika sonra geçersiz olur. Idle timeout 5–60 dakika arasında
  yapılandırılabilir ve access token süresini aşamaz.
- Her bearer isteğinde kullanıcı aktifliği, security stamp, oturumun iptal ve
  süre durumu backend tarafından yeniden doğrulanır.
- Logout backend oturumunu iptal eder ve tarayıcıdaki cookie'yi temizler.
- Yetkiler ayrı `permission` claim'leriyle backend politikalarına taşınır.
- Login ve logout BFF uçları cross-site istekleri reddeder.
- Giriş JSON gövdesi 8 KB ile sınırlıdır.

## İlk Yönetici

İlk yönetici yalnızca veritabanında hiç admin kullanıcısı yokken aşağıdaki
environment variable'larla oluşturulur:

- `Authentication__Bootstrap__Email`
- `Authentication__Bootstrap__DisplayName`
- `Authentication__Bootstrap__Password`

Parola en az 14 karakter olmalı; büyük harf, küçük harf, rakam ve sembol
içermelidir. İlk başarılı başlangıçtan sonra bootstrap değişkenleri secret
store'dan kaldırılmalıdır. Kaynak koda veya `.env.example` içine gerçek değer
yazılmaz.

JWT için ayrıca issuer, audience ve en az 32 karakterlik signing key gerekir.
Süreler aşağıdaki güvenli environment variable'larla değiştirilebilir:

- `Authentication__Jwt__AccessTokenMinutes`
- `Authentication__Jwt__IdleTimeoutMinutes`

Migration gerçek veritabanına ayrıca kontrollü deployment adımında
uygulanmalıdır; uygulama migrationı kendiliğinden çalıştırmaz.
