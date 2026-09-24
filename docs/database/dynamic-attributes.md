# Dinamik Ürün Özellikleri

## Amaç

Kategoriye göre değişen filtreleri hard-code etmeden yönetmek ve ürün değerlerini
arama, karşılaştırma ve import için güvenli tiplerde saklamak.

## Veri Tipleri

- `Text`: filtre dışı açıklayıcı teknik değerler
- `Number`: hacim, ağırlık, çap, uzunluk gibi sayısal değerler
- `Boolean`: evet/hayır özellikleri
- `Option`: malzeme, renk veya şekil gibi tek seçimli sözlükler
- `MultiOption`: kullanım amacı gibi çok seçimli sözlükler

## Varlıklar

| Varlık | Sorumluluk |
|---|---|
| AttributeDefinition | Kod, veri tipi ve opsiyonel birim boyutu |
| AttributeTranslation | TR/EN görünen özellik adı |
| AttributeOption | Kontrollü seçenek kodu ve sıralaması |
| AttributeOptionTranslation | TR/EN seçenek adı |
| UnitDefinition | Birim kodu, sembolü, boyutu ve temel birime dönüşüm katsayısı |
| CategoryAttributeAssignment | Kategoriye özel filtre/görünürlük kuralları |
| ProductAttributeValue | Typed ürün değeri ve ham import değeri |

## Typed Değer Kuralı

Her `ProductAttributeValue` satırında aşağıdakilerden tam olarak biri dolu olabilir:

- TextValue
- NumericValue
- BooleanValue
- AttributeOptionId

`UnitId` yalnızca NumericValue ile kullanılabilir. Veritabanı check constraint'leri
bu kuralları persistence seviyesinde de uygular.

## Çoklu Değerler

`Sequence`, aynı ürün ve attribute için birden fazla değerin sırasını belirler.
`ProductId + AttributeId + Sequence` birleşimi benzersizdir. Bu yapı
`MultiOption` ve gelecekte gerekebilecek sıralı ölçü değerlerini destekler.

## Birim Normalizasyonu

Her birim bir boyuta ve temel birime dönüşüm katsayısına sahiptir.

Örnek:

- `ML`, VOLUME, katsayı `1`, temel birim
- `L`, VOLUME, katsayı `1000`

`80 L`, normalize edildiğinde `80000 ML` olur. Orijinal `"80 litre"` metni
`RawValue` içinde import denetimi için korunabilir.

## Filtre ve Karşılaştırma

`CategoryAttributeAssignment` aşağıdaki davranışları kategori bazında yönetir:

- IsRequired
- IsFilterable
- IsVisibleOnProduct
- IsVisibleOnComparison
- SortOrder

Karşılaştırma feature flag'i kapalı olsa da veri modeli karşılaştırılabilir alanları
hazırlayabilir. Bu, özelliği production'da otomatik olarak etkinleştirmez.

## Index Stratejisi

- Attribute ve option kodlarında unique index
- Option filtreleri için AttributeId/OptionId/ProductId indexi
- Sayısal aralık filtreleri için AttributeId/NumericValue/ProductId indexi
- Kategori filtre ve karşılaştırma alanları için category/flag/sort indexleri
- Her boyutta yalnızca bir temel birim için filtered unique index

## Migration

`DynamicAttributes` migration kaynak kodu oluşturulmuştur. Hiçbir veritabanına
uygulanmamıştır.

## Import Material Sözleşmesi

Importtaki `Malzeme` kolonu aktif `MATERIAL` kodlu Option/MultiOption attribute'a
bağlanır. Türkçe option adı kesin ve tekil eşleşmelidir. Import bilinmeyen option
veya attribute kaydı oluşturmaz. Başarılı eşleşme typed `ProductAttributeValue`
olarak, `RawValue` korunarak yayınlanır.
