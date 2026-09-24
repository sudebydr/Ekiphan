# Teklif Listesi ve Gönderim Arayüzü

## Rotalar

- Ürün detayındaki **Teklif listeme ekle** kontrolü seçilen ürünü cihaz listesine ekler.
- `/teklif-listem` adet/not düzenleme, iletişim bilgileri ve izinlerle gönderim sunar.
- `/api/quotes` Next.js BFF rotası aynı-origin JSON isteğini backend
  `POST /api/quotes` endpointine iletir.

## Cihaz-local liste

Teklif listesi `ekiphan_quote_list_v1` anahtarıyla `localStorage` içinde tutulur.
Bu veri yalnızca cihaz-local kullanıcı tercihi niteliğindedir; ad, e-posta, telefon
veya başka kişisel veri içermez.

Saklanan yardımcı alanlar:

- Product GUID
- Public slug
- Görünen ürün adı ve SKU
- Opsiyonel marka adı
- Miktar ve ürün notu

LocalStorage değiştirilebilir bir istemci alanıdır ve güven kaynağı değildir.
Gönderimde frontend yalnızca `productId`, opsiyonel `variantId`, miktar ve notu
backend'e yollar. Backend ürün adı, SKU, marka ve görsel snapshot'ını yayımlanmış
katalogdan yeniden üretir. Cihazdaki ad/SKU alanları yalnızca ekran sunumu içindir.

Liste okuma sırasında:

- Şema ve GUID biçimi doğrulanır.
- Metin/miktar sınırları yeniden uygulanır.
- En fazla 50 geçerli kayıt kabul edilir.
- Bozuk JSON veya browser storage erişim hatası güvenli boş listeye dönüşür.
- Yazma başarısızlığı kullanıcıya bildirilir; sessizce kayıt yapılmış sayılmaz.

## BFF güvenlik sınırı

Next.js `/api/quotes` rotası:

- Yalnızca `POST` ve `application/json` kabul eder.
- Origin/host uyuşmazlığını reddeder.
- Content-Length ve gerçek body boyutunu 64 KB ile sınırlar.
- Backend adresini server-side environment değerinden okur.
- Backend hata gövdesini cache'lemeden iletir.
- Bağlantı hatalarında hassas altyapı ayrıntısı göstermez.

## İzin kapısı

`NEXT_PUBLIC_KVKK_NOTICE_URL`, hukuk tarafından onaylanmış aydınlatma metninin
on-site yolu veya mutlak HTTPS adresidir. Değer eksik/geçersizse:

- KVKK checkbox'ı devre dışıdır.
- Gönderim düğmesi devre dışıdır.
- Kullanıcıya listenin cihazda korunacağı açıklanır.

Backend ayrıca `QuoteConsent__KvkkVersion` ve
`QuoteConsent__CommercialCommunicationVersion` değerleri olmadan `503` döndürür.
Frontend URL'si metnin görünürlüğünü, backend sürüm değerleri ise alınan iznin
denetlenebilir snapshot'ını sağlar; production'da birlikte yönetilmelidir.

Ticari iletişim onayı teklif gönderiminden bağımsız ve opsiyoneldir.

## Erişilebilirlik

- Tüm form alanları görünür label ve uygun autocomplete değerleri taşır.
- Hata, başarı ve cihaz saklama durumları `alert`/`status` live region'larıyla bildirilir.
- Ürün kaldırma butonları ürün adını içeren erişilebilir etikete sahiptir.
- Klavye odak göstergeleri korunur.
- Mobilde ürün alanları ve iletişim formu tek kolona düşer.
