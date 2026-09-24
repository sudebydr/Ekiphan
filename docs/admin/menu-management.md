# Menü Yönetimi

Admin panelindeki `/admin/content/menu` ekranı, header ve footer bağlantılarını
`content.manage` yetkisiyle yönetir.

## Alanlar

- `code`: Değişmeyen teknik kimlik; en fazla 100 ASCII harf, rakam veya `_`.
- `location`: Yalnızca `Header` veya `Footer`.
- `url`: Site içi bağlantıda `/` ile başlayan yol veya `#` ile başlayan anchor.
  Harici bağlantıda yalnızca kullanıcı bilgisi içermeyen mutlak HTTPS adresi.
- `parentId`: Aynı konumdaki üst menü kaydı. En fazla üç seviye desteklenir.
- `sortOrder`: Aynı seviyedeki görünüm sırası.
- `translations`: Benzersiz `tr` ve/veya `en` etiketi.
- `isPublished`: Yalnız çevirisi bulunan kayıtlar yayımlanabilir.

Harici bağlantılar güvenlik nedeniyle her zaman yeni sekmede açılır. Yayımlanmış
bir alt kayıt yalnız yayımlanmış bir üst kayda bağlanabilir; döngüsel hiyerarşi
reddedilir.

## Güvenlik ve dayanıklılık

Admin çağrıları frontend BFF üzerinden yapılır. Access token JavaScript'e
verilmez; HttpOnly cookie'den okunur. API tarafında JWT yapılandırması eksikse
admin uçları anonim erişime düşmek yerine `503` döndürür.

Public header verisi alınamazsa veya henüz yayımlanmış kayıt yoksa, site
sayfaları derleme sırasında tanımlı sınırlı bağlantı listesini kullanır. Böylece
veritabanı kesintisi temel gezinmeyi kullanılmaz hale getirmez. API'den gelen URL
frontend'de de yeniden doğrulanır.

## Yayına alma

`ManagedNavigation` migration'ı uygulanmadan admin menü kayıtları
kullanılamaz. Migration'ın üretim veritabanına uygulanması ayrı ve kontrollü
bir dağıtım adımıdır.
