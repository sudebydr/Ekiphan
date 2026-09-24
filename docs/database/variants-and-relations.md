# Ürün Varyantları ve İlişkileri

## Varyant Modeli

Bir ürün en fazla iki varyant grubuna sahip olabilir.

Örnek:

- Grup 1: Renk
- Grup 2: Ölçü

Her grup çevrilebilir seçenekler içerir. Bir `ProductVariant`, her gruptan tam
olarak bir seçenek seçer ve kendine ait SKU taşır.

## Varlıklar

| Varlık | Sorumluluk |
|---|---|
| ProductVariantGroup | Ürüne bağlı varyant ekseni |
| ProductVariantGroupTranslation | TR/EN grup adı |
| ProductVariantOption | Gruba bağlı seçilebilir değer |
| ProductVariantOptionTranslation | TR/EN seçenek adı |
| ProductVariant | SKU taşıyan varyant kombinasyonu |
| ProductVariantSelection | Varyantın grup/seçenek eşleşmesi |

## Varyant İş Kuralları

- Bir üründe en fazla iki grup olabilir.
- Grup kodları ürün içinde benzersizdir.
- Option kodları grup içinde benzersizdir.
- Bir varyant her gruptan tam bir seçenek seçmelidir.
- Seçenek seçildiği gruba ait olmalıdır.
- Aynı kombinasyon ikinci kez oluşturulamaz.
- Varyant SKU'su normalize edilir ve veritabanında benzersizdir.
- Varyantlar silinmeden pasif duruma alınabilir.

## Ürün İlişkileri

Desteklenen tipler:

- Similar
- Complementary
- Accessory
- Alternative

Her ilişki kaynak ve hedef ürün taşır. `IsBidirectional`, ilişkinin ters yönden de
okunup okunmayacağını açıkça belirtir. Sistem örtük olarak ikinci bir kayıt üretmez.

`Origin` alanı `Manual` veya `Automatic` olabilir. Otomatik öneri geliştirilmiş veya
production'da etkinleştirilmiş değildir; alan gelecekteki skor altyapısı için
izlenebilirlik sağlar.

Public ürün detayı aktif `Similar` ve `Complementary` ilişkilerini okur. İlişkili
ürün yayımlanmış ve istenen dilde çevrilmiş olmalıdır. `IsBidirectional` ilişkiler
hedef üründen görüntülendiğinde de sonuç üretir. Cevap boyutunu ve sorgu maliyetini
sınırlamak için her ilişki türünde ilk 12 kayıt sunulur.

Admin tarafında manuel ilişkiler `catalog.manage` izniyle aranabilir, listelenebilir,
oluşturulabilir ve pasifleştirilebilir. Pasif manuel kayıt aynı bileşimle yeniden
eklendiğinde fiziksel duplicate oluşturmak yerine yeni yön/sıra ayarlarıyla
etkinleştirilir. Otomatik kökenli kayıtlar admin manuel işlemleriyle değiştirilemez.

## İlişki Güvenceleri

- Ürün kendisiyle ilişkilendirilemez.
- Kaynak/hedef/tip birleşimi benzersizdir.
- İlişkiler silinmeden pasifleştirilebilir.
- Kaynak ve hedef sorguları için ayrı indexler bulunur.
- Ürün silme işlemi aktif ilişkiler varken `Restrict` davranışı gösterir.

## Migration

`VariantsAndRelations` migration kaynak kodu oluşturulmuştur. Veritabanına
uygulanmamıştır.
