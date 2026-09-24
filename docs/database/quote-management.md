# Teklif Yönetimi

## Faz-1 Yaklaşımı

Teklif listesi e-ticaret sepeti değildir. Fiyat, ödeme ve online sipariş bu modülün
kapsamında bulunmaz. Ziyaretçi birden fazla ürün ve varyant için iletişim/teklif
talebi oluşturur.

## Varlıklar

| Varlık | Sorumluluk |
|---|---|
| QuoteRequest | İletişim, izinler, genel mesaj ve durum |
| QuoteRequestItem | Ürün/varyant snapshot ve miktar |
| QuoteStatusHistory | Her durum değişikliğinin geçmişi |
| QuoteRequestAttachment | Güvenli medya kütüphanesi eki |

## Snapshot Alanları

Ürün daha sonra değişse veya soft-delete edilse bile teklif satırında şunlar korunur:

- Ürün adı
- SKU
- Marka adı
- Miktar
- Varyant açıklaması
- Ürün notu
- Görsel için güvenli storage key
- Opsiyonel orijinal ProductId ve VariantId referansları

## İzinler

- KvkkConsentAt zorunludur.
- CommercialCommunicationConsentAt opsiyoneldir ve KVKK izninden ayrı saklanır.
- KVKK ve opsiyonel ticari iletişim izin metni sürümleri zaman damgalarıyla birlikte
  snapshot olarak saklanır.
- Ticari iletişim zaman damgası ve sürümü ya birlikte dolu ya birlikte boş olmak
  zorundadır; kural domain ve veritabanı check constraint'i ile korunur.

## Durum Akışı

```text
New -> Reviewing -> Contacted -> Preparing -> Sent -> Won
                              \              \      -> Lost
                               -> Lost
Her aktif aşama -> Archived
Won/Lost -> Archived
```

İzin verilmeyen geçişler domain tarafından reddedilir. Her başarılı geçişte önceki
durum, yeni durum, zaman, opsiyonel admin kullanıcı kimliği ve not kaydedilir.

## Veri Bütünlüğü

- RequestNumber benzersizdir.
- Miktar sıfırdan büyük olmak zorundadır.
- Aynı ürün/varyant/SKU aynı teklife iki kez eklenemez.
- Gönderim doğrulamasında en az bir satır bulunmalıdır.
- Kullanımdaki medya eki `Restrict` FK nedeniyle silinemez.
- Ürün ve varyant FK'leri snapshot güvenliği için `Restrict` davranışındadır.
- `RowVersion`, iki adminin aynı eski kayıt üzerinden birbirinin durum değişikliğini
  ezmesini engeller.

## Henüz Uygulanmayanlar

- reCAPTCHA/bot doğrulaması
- E-posta notification adapter
- Admin liste/detay ekranları
- KVKK retention ve anonimleştirme job'ı

SMTP ayarları olmadan bildirim gönderilmiş kabul edilmez.

## Uygulanan Public Gönderim

- `POST /api/quotes` yalnızca yayımlanmış ürünleri ve aktif varyantları kabul eder.
- Ürün adı, SKU, marka, varyant ve görsel snapshot'ları istemciden değil katalogdan
  sunucu tarafında üretilir.
- En fazla 50 farklı ürün/varyant satırı ve satır başına 1–100.000 miktar kabul edilir.
- Request number tarih ile 64-bit kriptografik rastgele suffix'ten oluşur; kişisel
  veri veya sıralı sayaç taşımaz.
- IP başına 15 dakikada beş gönderim sınırı, 64 KB body limiti ve görünmez honeypot
  alanı uygulanır.
- Onaylı izin metni sürümleri yapılandırılmamışsa endpoint `503` ile kapalı kalır.

Harici bot sağlayıcısı ve gerçek secret seçilene kadar rate limit ile honeypot temel
korumadır. Production açılış kapısında sağlayıcı doğrulaması eklenmelidir.

## Uygulanan Admin API

- `quotes.manage` izni olmadan kişisel veri içeren teklif listesi ve detayı açılamaz.
- JWT yapılandırılmamışsa endpointler anonim moda düşmek yerine `503` döner.
- Liste durum ve en fazla 100 karakterlik talep no/ad/firma/e-posta aramasıyla
  sayfalanabilir.
- Detay iletişim, izin snapshot'ları, ürün satırları ve tüm durum geçmişini döndürür.
- Yanıtlar `private, no-store` ve `Pragma: no-cache` taşır.
- Durum değişikliğinde admin `sub` claim'i GUID olarak geçmişe yazılır.
- İstemci son okuduğu Base64 `version` değerini gönderir. Kayıt başka kullanıcı
  tarafından değişmişse `409 Conflict` döner.
- Domain durum makinesinde izin verilmeyen geçişler yine `409` ile reddedilir.

## Migration

`QuoteManagement`, `TagsAndQuoteConsentVersions` ve `QuoteConcurrency` migration
kaynak kodları oluşturulmuştur. Veritabanına uygulanmamıştır.
