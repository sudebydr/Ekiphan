# HTTP Güvenlik ve Operasyon Sözleşmesi

## Correlation kimliği

Backend her cevapta `X-Correlation-ID` döndürür ve değeri yapılandırılmış log
scope'una ekler. İstemci değeri yalnızca 8–64 karakterlik ASCII harf, rakam, `-`
ve `_` kümesine uyuyorsa kabul edilir. Correlation kimliğine kişisel veri, token,
e-posta veya başka iş verisi yazılmamalıdır.

## Sağlık kontrolleri

- `/api/health` ve `/api/health/live`: yalnızca uygulama sürecini sınayan liveness.
- `/api/health/ready`: SQL bağlantısını sınayan readiness; başarısızlıkta `503`.

Kubernetes veya benzeri orkestratörlerde liveness yeniden başlatma kararı,
readiness ise trafik yönlendirme kararı için kullanılmalıdır. Sağlık gövdesi
exception ve bağlantı bilgisi içermez, ayrıca cache'lenmez.

## Güvenlik başlıkları

API; `nosniff`, frame engelleme, `no-referrer`, kısıtlı Permissions Policy ve API
için bütün içerik kaynaklarını kapatan Content Security Policy uygular. Frontend
aynı temel başlıkları ve Next.js'in çalışması için gereken kaynak türlerini
allowlist eden ayrı bir CSP uygular. Production dışı frontend CSP'si geliştirme
araçlarını bozmamak için etkin değildir.

TLS sonlandıran reverse proxy, `X-Correlation-ID`, `Retry-After` ve güvenlik
başlıklarını upstream cevabından korumalı; aynı başlıkları çelişkili değerlerle
yeniden yazmamalıdır. HSTS yalnızca production backend hattında HTTPS ile
etkinleştirilir.

## Rate limit cevabı

Limit aşımı `application/problem+json` içerik türünde `429` döndürür.
Limiter yeniden deneme süresi sağlıyorsa saniye cinsinden `Retry-After` eklenir.
Cevapta correlation kimliği bulunur ve iç altyapı ayrıntısı yer almaz.
