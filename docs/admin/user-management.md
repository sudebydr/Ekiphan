# Admin Kullanıcı ve İzin Yönetimi

Admin ekranı: `/admin/users`

Yalnızca `users.manage` izni bulunan yöneticiler kullanıcı listesini görebilir,
yeni yönetici oluşturabilir, profil/aktiflik/izinleri güncelleyebilir ve parola
yenileyebilir.

Desteklenen izinler:

- `catalog.manage`
- `quotes.manage`
- `imports.manage`
- `imports.publish`
- `media.manage`
- `users.manage`

## Güvenlik Kuralları

- E-posta benzersiz ve geçerli olmalıdır.
- Her kullanıcı en az bir desteklenen izne sahip olmalıdır.
- Parola 14–256 karakter olmalı; büyük/küçük harf, rakam ve sembol içermelidir.
- Son aktif `users.manage` yöneticisi pasife alınamaz veya bu izni kaybedemez.
- Profil, izin, aktiflik ve parola değişiklikleri security stamp değerini yeniler.
- Kısa ömürlü mevcut tokenlar en geç token süresi dolduğunda geçersiz olur.
- API yanıtları `private, no-store`; yazma istekleri same-origin ve boyut
  kontrolleriyle korunur.

Kullanıcılar fiziksel olarak silinmez; erişim `IsActive=false` ile kapatılır.

