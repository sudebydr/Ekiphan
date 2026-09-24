# Varyant Yönetimi

Admin ekranı: `/admin/catalog/variants`

## İş akışı

1. Ürün seçilir.
2. En fazla iki varyant grubu tanımlanır.
3. Gruplara Türkçe ve opsiyonel İngilizce seçenekler eklenir.
4. Her gruptan tam bir aktif seçenek seçilerek benzersiz varyant SKU'su oluşturulur.

Varyant SKU'ları oluşturulduktan sonra yeni grup eklenemez. Böylece mevcut
kombinasyonların eksik eksenle kalması engellenir. Seçenekler ve varyantlar
fiziksel olarak silinmek yerine pasifleştirilir.

## Güvenceler

- Bir üründe en fazla iki grup
- Ürün içinde benzersiz grup kodu
- Grup içinde benzersiz seçenek kodu
- Sistem genelinde benzersiz varyant SKU
- Her gruptan tam bir seçenek
- Aynı seçenek kombinasyonunun tekrar oluşturulamaması
- Başka gruba ait veya yeni seçimde pasif seçeneğin kullanılamaması
- Mevcut varyantın pasifleştirilmiş seçimini koruyarak SKU/durum güncelleyebilmesi

## API

- `GET|POST /api/admin/catalog/products/{productId}/variants`
- `PUT /api/admin/catalog/products/{productId}/variants/{variantId}`
- `POST /api/admin/catalog/products/{productId}/variant-groups`
- `PUT /api/admin/catalog/products/{productId}/variant-groups/{groupId}`
- `POST /api/admin/catalog/products/{productId}/variant-groups/{groupId}/options`
- `PUT /api/admin/catalog/products/{productId}/variant-options/{optionId}`

Yollar `catalog.manage` yetkisi, katalog yönetimi rate limitleri, 24 KB JSON
sınırı, private/no-store cevapları ve same-origin korumalı BFF üzerinden çalışır.
Mevcut varyant tabloları kullanıldığı için migration gerekmez.
