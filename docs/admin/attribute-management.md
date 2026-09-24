# Dinamik Özellik Yönetimi

Admin ekranı: `/admin/catalog/attributes`

## Kapsam

- Text, Number, Boolean, Option ve MultiOption özellik tanımları
- Türkçe ve opsiyonel İngilizce özellik adları
- Sayısal özellikler için birim boyutu
- Option/MultiOption özellikleri için sıralı ve çok dilli seçenek sözlüğü
- Kategori bazında zorunluluk, filtre, ürün görünürlüğü ve karşılaştırma kuralları
- Fiziksel silme yerine aktif/pasif yaşam döngüsü

Mevcut bir özelliğin veri tipi değiştirilemez. Bu kural, kaydedilmiş tipli ürün
değerlerinin anlamının bozulmasını önler.

## API

- `GET /api/admin/catalog/attributes`
- `POST /api/admin/catalog/attributes`
- `PUT /api/admin/catalog/attributes/{id}`
- `POST /api/admin/catalog/attributes/{id}/options`
- `PUT /api/admin/catalog/attribute-options/{id}`
- `PUT /api/admin/catalog/category-attributes`
- `DELETE /api/admin/catalog/category-attributes/{categoryId}/{attributeId}`

## Ürün editörü entegrasyonu

Ürün editörü seçilen kategorilerin özellik kurallarını birleştirir ve veri tipine
göre uygun kontrolü üretir. Bir özellik birden fazla seçili kategoride yer alırsa
tek kez gösterilir; atamalardan herhangi biri zorunluysa alan zorunlu kabul edilir.
Sayısal alanlarda yalnızca eşleşen boyuttaki aktif birimler, seçenek alanlarında
yalnızca ilgili özelliğin aktif seçenekleri sunulur.

Yollar `catalog.manage` yetkisi, katalog rate limitleri, private/no-store cevaplar
ve same-origin korumalı frontend BFF üzerinden çalışır. Mevcut dinamik özellik
tabloları kullanıldığı için migration gerekmez.
