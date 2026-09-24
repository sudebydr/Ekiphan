# Ortak Staging Ortamı

Bu ortam iki geliştiricinin aynı frontend, API, veritabanı ve admin
hesaplarını kullanması içindir.

## Hedef Mimari

- Frontend ve admin BFF: Vercel üzerinde Next.js
- Backend: Azure App Service üzerinde ASP.NET Core API
- Veritabanı: Azure SQL Database
- Kaynak ve deployment tetikleyicisi: GitHub `staging` dalı

Gerçek bağlantı dizeleri, parolalar, JWT anahtarları ve deployment profilleri
repository içine yazılmaz.

## 1. Azure SQL

1. Azure'da `EkiphanStaging` adında boş bir SQL veritabanı oluşturun.
2. Kurulum süresince yalnızca uygulama ve migration çalıştıracak istemciler için
   ağ erişimi verin.
3. Bağlantı dizesini yerel bir secret olarak tanımlayıp migration'ları uygulayın:

   ```bash
   dotnet tool restore
   dotnet ef database update \
     --project src/backend/Ekiphan.Infrastructure/Ekiphan.Infrastructure.csproj \
     --startup-project src/backend/Ekiphan.Api/Ekiphan.Api.csproj \
     --context EkiphanDbContext
   ```

Migration komutu `ConnectionStrings__EkiphanDatabase` environment variable'ını
kullanır. Bağlantı dizesini komut geçmişine veya repository dosyalarına
yazmayın.

## 2. Azure App Service API

Linux üzerinde .NET 8 kullanan bir App Service oluşturun. Aşağıdaki değerleri
App Service ortam ayarlarında tanımlayın:

| Ayar | Açıklama |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| `Authentication__Jwt__Issuer` | Staging API issuer değeri |
| `Authentication__Jwt__Audience` | Staging admin audience değeri |
| `Authentication__Jwt__SigningKey` | En az 32 karakterlik rastgele secret |
| `Authentication__Jwt__AccessTokenMinutes` | Önerilen: `60` |
| `Authentication__Jwt__IdleTimeoutMinutes` | Önerilen: `30` |
| `QuoteConsent__KvkkVersion` | Onaylı KVKK metni sürümü |
| `QuoteConsent__CommercialCommunicationVersion` | Ticari iletişim metni sürümü |
| `PublicMedia__BaseUrl` | Kalıcı medya alanının HTTPS adresi |
| `MediaStorage__LocalRoot` | Yalnızca geçici staging depolaması kullanılacaksa kalıcı App Service yolu |

Azure App Service'in **Connection strings** bölümünde adı
`EkiphanDatabase`, türü `SQLAzure` olan bağlantıyı tanımlayın.

İlk admin hesabını yalnızca ilk başarılı başlangıçta oluşturmak için geçici
olarak şu ayarları ekleyin:

- `Authentication__Bootstrap__Email`
- `Authentication__Bootstrap__DisplayName`
- `Authentication__Bootstrap__Password`

Admin hesabı oluştuktan sonra üç bootstrap ayarını App Service'ten kaldırın.

## 3. GitHub API Deployment

GitHub repository'sinde `staging` environment'ını oluşturun ve şunları ekleyin:

- Environment variable: `AZURE_STAGING_WEBAPP_NAME`
- Environment secret: `AZURE_STAGING_PUBLISH_PROFILE`

`.github/workflows/deploy-api-staging.yml`, `staging` dalındaki backend
değişikliklerini test eder ve Azure App Service'e gönderir. Workflow ayrıca
GitHub Actions ekranından elle çalıştırılabilir.

## 4. Vercel Frontend

Vercel projesini aynı GitHub repository'sine bağlayın:

- Root Directory: `src/frontend/ekiphan-web`
- Production Branch: `staging`
- Framework Preset: Next.js

Vercel staging/production environment'ında aşağıdaki değerleri tanımlayın:

| Ayar | Değer |
| --- | --- |
| `EKIPHAN_API_BASE_URL` | Azure API'nin HTTPS origin'i |
| `NEXT_PUBLIC_API_BASE_URL` | Azure API'nin HTTPS origin'i |
| `EKIPHAN_SITE_URL` | Vercel staging sitesinin HTTPS origin'i |
| `NEXT_PUBLIC_KVKK_NOTICE_URL` | Onaylı KVKK sayfası veya HTTPS adresi |
| `SHOWROOM_TOUR_URL` | Varsa onaylı HTTPS sanal tur adresi |

`EKIPHAN_API_BASE_URL` admin BFF tarafından sunucu tarafında kullanılır. JWT
ve veritabanı secret'ları Vercel'e verilmez.

## 5. Doğrulama

Deployment sonrasında sırayla kontrol edin:

1. Azure API: `/api/health/live`
2. Azure API veritabanı: `/api/health/ready`
3. Vercel frontend ana sayfası
4. Vercel `/admin/login` girişi
5. `/api/admin/auth/session` üzerinden oturum
6. Admin dashboard, katalog, içerik, teklif ve iletişim ekranları

Dosya yükleme özelliği mevcut durumda tehdit tarayıcısı hazır olmadığında
güvenli biçimde kapalı kalır. Ortak medya yükleme özelliği açılmadan önce kalıcı
object storage ve çalışan bir tehdit tarayıcısı bağlanmalıdır.
