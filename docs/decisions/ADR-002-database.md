# ADR-002 - Veritabanı Seçimi

- Durum: Accepted
- Tarih: 2026-07-28

## Bağlam

FSD/BRD/SAD SQL Server 2019/2022 önerir. Repository'de mevcut veritabanı veya
deployment kısıtı yoktur. Katalog yaklaşık 20.000 ürün ve dinamik filtreler içerir.

## Önerilen Karar

Hedef altyapı doğrulanırsa SQL Server ve Entity Framework Core Code First kullanmak.

## Gerekçe

- Kaynak çözüm mimarisiyle uyum
- Güçlü ilişkisel bütünlük ve index desteği
- .NET/EF Core ile olgun entegrasyon
- 20.000 ürün için doğru şema ve indexlerle yeterli kapasite

## Alternatif

PostgreSQL teknik olarak uygundur; hosting/lisans/operasyon avantajı kanıtlanırsa
bu ADR güncellenebilir.

## Kabul Notu

SQL Server uygulama geliştirme hedefi olarak kabul edildi. Hosting sağlayıcısı,
lisans, backup, restore testi ve retention politikası deployment öncesi kesinleşecektir.
