# Admin Medya Upload

## Endpoint

`POST /api/admin/media` yalnız `permission=media.manage` claim'iyle kullanılabilir.
JWT yapılandırılmamışsa `503`, kimlik yoksa `401`, izin yoksa `403` döner. Kullanıcı
veya IP başına beş dakikada on istek sınırı vardır.

Multipart alanları:

| Alan | Kural |
|---|---|
| `file` | Tam olarak bir dosya |
| `assetType` | `Image`, `Pdf` veya `Document`; numeric enum kabul edilmez |
| `languageCode` | `tr` veya `en` |
| `title` | Zorunlu, en fazla 250 karakter |
| `altText` | Görsellerde zorunlu, en fazla 500 karakter |

Başarılı cevap `201` ile medya kimliği, güvenli storage key, MIME, boyut ve SHA-256
checksum döndürür. Dosya içeriği veya storage fiziksel yolu cevapta yer almaz.

## Fail-closed çalışma

Tehdit tarayıcısı veya storage yapılandırılmamışsa dosya kabul edilmez. Varsayılan
uygulama gerçek bir malware sağlayıcısı varmış gibi davranmaz ve `503` döndürür.
Admin BFF/UI, gerçek tarama sağlayıcısı seçilip adapter bağlandıktan sonra bu
endpoint üzerine eklenmelidir.
