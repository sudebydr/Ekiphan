# Teklif Yönetimi Ekranı

Admin teklif ekranı `/admin/quotes` adresindedir. `quotes.read` veya
`quotes.manage` yetkili kullanıcılar talepleri sayfalı görüntüler. Yalnızca
`quotes.manage` sahipleri durum, atama ve dahili not işlemi yapabilir.

## Güvenlik sınırı

Browser backend API'ye veya bearer token'a doğrudan erişmez. Next.js BFF
`/api/admin/quotes/*` isteklerinde yalnızca server-side okunabilen
`ekiphan_admin_access_token` adlı `HttpOnly` cookie'yi Authorization başlığına
dönüştürür.

- Token browser JavaScript, localStorage veya sessionStorage'a yazılmaz.
- Yalnızca liste, assignee listesi, geçerli GUID detay ve `{quoteId}/status`,
  `{quoteId}/assignment`, `{quoteId}/notes` rotaları allowlist'tedir.
- Durum değişikliklerinde origin/host eşleşmesi ve `application/json` zorunludur.
- Content-Length ve gerçek body 16 KB ile ayrı ayrı sınırlandırılır.
- Backend adresi yalnızca server-side `EKIPHAN_API_BASE_URL` değerinden okunur.
- PII yanıtları `private, no-store` ve `Pragma: no-cache` taşır.
- Backend bağlantı hataları altyapı ayrıntısı sızdırmadan maskelenir.

Kimlik sağlayıcı callback'i uygulanana kadar cookie elle üretilmemelidir. Entegrasyon
kısa ömürlü token içinde `quotes.manage` iznini sağlamalı; cookie `HttpOnly`,
`Secure`, `SameSite=Lax` ve uygun dar `Path` ayarlarıyla oluşturulmalıdır.

## Kullanıcı akışı

1. Liste en yeni talepleri önce gösterir.
2. Kullanıcı arama, tarih, durum, atanan kullanıcı, ürün/SKU, yalnızca yeni,
   sıralama ve sayfa boyutu filtrelerini uygular. Filtreler URL query'sinde korunur.
3. Detay ekranında iletişim/proje bilgileri, KVKK ve ticari iletişim izin
   zaman+sürümleri, ürün snapshot'ları ve kronolojik durum geçmişi gösterilir.
4. UI yalnızca mevcut durumdan domain tarafından izin verilen hedef durumları sunar.
5. Durum değişikliği admin kimliğiyle geçmişe yazılır; bağımsız dahili notlar yazar
   ve zaman bilgisiyle saklanır.
6. Aktif teklif operatörleri arasından atama yapılır; geçersiz kullanıcı reddedilir.

## Eşzamanlılık

Liste ve detay yanıtındaki Base64 `version`, SQL rowversion'ın opaque temsilidir.
UI bu alanı kullanıcıya düzenletmez. Durum değişikliğinde son okunan sürüm backend'e
gönderilir. Başka bir admin kaydı değiştirdiyse backend `409` döndürür; ekran detay
ve listeyi otomatik yeniler, kullanıcının yeni durumu inceleyip tekrar karar vermesini
sağlar.

## PII ve erişilebilirlik

- Liste yanıtı e-posta ve telefonu maskeli döndürür; tam iletişim bilgisi yalnızca
  yetkili detay endpointinde bulunur.
- Kişisel veri browser storage'a yazılmaz; yalnızca React ekran state'inde tutulur.
- Arama en fazla 100, durum notu en fazla 1.000 karakterdir.
- Hata ve başarılar canlı `alert`/`status` bölgeleriyle bildirilir.
- Tablolar gerçek header hücreleri, durumlar metinli badge ve formlar görünür label
  kullanır.
- Mobil görünümde detay ve durum formu tek kolona düşer.

Oturum yoksa ekran güvenli `401`; backend/JWT yapılandırması eksikse `503` durumunu
gösterir ve herhangi bir PII render etmez.
