# Ürün Etiketleri

## Amaç

Ürünlerin kontrollü, çok dilli ve filtrelenebilir etiketlerle ilişkilendirilmesini
sağlamak; import sırasında serbest metinden kontrolsüz etiket oluşmasını önlemek.

## Model

- `Tag`: benzersiz teknik kod, aktiflik ve audit zamanları.
- `TagTranslation`: Türkçe/İngilizce ad ve dil içinde benzersiz slug.
- `ProductTag`: ürün-etiket many-to-many ilişkisi ve sıralama.

## Kurallar

- Tag kodu normalize edilerek büyük harf saklanır ve benzersizdir.
- Yalnızca `tr` ve `en` çevirileri kabul edilir.
- Aynı tag üzerinde aynı dil iki kez eklenemez.
- Ürün aynı tag'e iki kez bağlanamaz.
- Kullanımdaki tag silinirse ilişki cascade ile kaybolmaz; silme `Restrict`tir.
- Ürün silinirse join kayıtları cascade ile kaldırılır.

## Import

Import `Etiket`/`Etiketler` alanı `;` veya `|` ile ayrıştırılır. Her değer yalnızca
aktif tag'in Türkçe adıyla kesin ve tekil eşleşirse kabul edilir.

- Bulunamayan değer: `UNKNOWN_TAG`
- Birden fazla tag'e karşılık gelen ad: `AMBIGUOUS_TAG`

Import yeni tag oluşturmaz. Çözülen ID'ler normalized payload'a yazılır ve ürün
yayınlanırken kaynak sırasıyla `ProductTag.SortOrder` atanır.

## Migration

`TagsAndQuoteConsentVersions` migration'ı `Tags`, `TagTranslations` ve `ProductTags`
tablolarını, gerekli
foreign key ve indeksleri oluşturur. Migration herhangi bir veritabanına
uygulanmamıştır.
