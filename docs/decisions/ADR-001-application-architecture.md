# ADR-001 - Uygulama Mimarisi

- Durum: Accepted
- Tarih: 2026-07-28

## Bağlam

Sistem public katalog, admin, import, teklif, içerik ve ileride ERP/CRM adaptörleri
içerecektir. Faz-1 bütçesi ve ekip büyüklüğü mikroservis operasyon maliyetini
haklı çıkarmamaktadır.

## Önerilen Karar

- Backend .NET 8 ASP.NET Core Web API
- Frontend Next.js/TypeScript
- Backend içinde modüler monolit
- Modül sınırlarında application service ve interface'ler
- Tek ilişkisel veritabanı; modüller arası kontrollü erişim

## Sonuçlar

Olumlu:

- Tek deployment ve daha düşük operasyon maliyeti
- Transaction gerektiren katalog/import/teklif akışlarında sadelik
- Faz-2 adaptörleri için açık servis sınırları

Olumsuz:

- Modül sınırları kod incelemesiyle korunmalıdır.
- Frontend/API ayrımı auth ve deployment kararlarını gerektirir.

## Kabul Notu

2026-07-28 tarihli geliştirmeye devam onayıyla uygulama başlangıcı için kabul edildi.
Hedef hosting ve admin auth modeli ayrı kararlarda kesinleştirilecektir.
