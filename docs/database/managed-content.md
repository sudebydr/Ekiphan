# Yönetilebilir İçerik Veri Modeli

`ContentPages` dil bağımsız kodu, yaşam döngüsü durumunu ve yayın zamanını
tutar. `ContentPageTranslations` Türkçe/İngilizce içerik ve SEO alanlarını
`ContentPageId + LanguageCode` bileşik anahtarıyla saklar.

Veri bütünlüğü:

- içerik kodu benzersizdir,
- dil ve slug birleşimi benzersizdir,
- dil kodu en fazla iki karakterdir,
- canonical URL yalnız site-relative veya HTTPS olabilir,
- status/updated-at indeksi public sorguları destekler.

Model `20260729101321_ManagedContent` migration'ında tanımlanmıştır. Migration
kaynağı oluşturulmuş, gerçek veritabanına uygulanmamıştır.

