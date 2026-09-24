# Yönetilebilir Navigasyon Veri Modeli

`ManagedNavigation` migration'ı iki tablo ekler:

## MenuItems

- `Id`: GUID birincil anahtar.
- `Code`: Teknik ve benzersiz menü kodu.
- `Location`: Header veya Footer enum değeri.
- `Url`, `IsExternal`, `OpenInNewTab`: Doğrulanmış hedef davranışı.
- `ParentId`: Aynı tabloya opsiyonel üst kayıt ilişkisi; silmede `Restrict`.
- `SortOrder`, `IsPublished`: Görünüm sırası ve yayın durumu.
- Standart `CreatedAt` ve `UpdatedAt` denetim alanları.

`Code` benzersiz indekslidir. Public sorgular için
`Location, IsPublished, SortOrder` bileşik indeksi, hiyerarşi için `ParentId`
indeksi bulunur.

## MenuItemTranslations

`MenuItemId, LanguageCode` bileşik birincil anahtarını kullanır. Etiket en fazla
100 karakterdir ve yalnız `tr` veya `en` domain kuralıyla kabul edilir.
Üst menü silinirse çevirileri cascade ile silinir.

Public projeksiyon yalnız yayımlanmış, istenen dilde etiketi bulunan kayıtları
döndürür. Görünür bir üst kaydı olmayan alt kayıtlar sonuçtan çıkarılır.
