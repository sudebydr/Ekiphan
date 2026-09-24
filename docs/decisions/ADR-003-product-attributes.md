# ADR-003 - Dinamik Ürün Özellikleri

- Durum: Accepted
- Tarih: 2026-07-28

## Bağlam

Bardak, tencere ve endüstriyel cihazlarda farklı filtreler gerekir. Tamamen sabit
kolonlar genişlemeyi engeller; tüm değerleri metin EAV olarak saklamak ise veri
kalitesi ve sorgu performansını bozar.

## Önerilen Karar

Typed dynamic attribute yaklaşımı:

- Attribute veri tipi taşır.
- CategoryAttribute görünürlük, filtre ve karşılaştırma davranışını tanımlar.
- Option değerleri kontrollü ve çevrilebilir sözlüklerden gelir.
- Sayısal değer ve birim ayrı tutulur.
- Ham import değeri audit amacıyla korunur.
- Sık kullanılan filtreler için hedefli index/denormalized read model ölçüm sonrası
  eklenebilir.

## Sonuçlar

- Kategoriye özel filtreler hard-code edilmez.
- Karşılaştırma aynı attribute kimlikleri üzerinden yapılabilir.
- Import sırasında birim ve alias normalizasyonu zorunlu hale gelir.

## Açık Kararlar

- AND/OR filtre semantiği
- Birim dönüşüm ve gösterim kuralları
- Attribute mirası: üst kategoriden alt kategoriye davranış
- Çoklu değerlerin sıralama ve facet count davranışı
