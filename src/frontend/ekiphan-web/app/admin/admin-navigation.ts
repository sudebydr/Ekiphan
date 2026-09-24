export type AdminNavigationItem = {
  label: string;
  href: string;
  description: string;
  permission?: string;
  anyPermission?: readonly string[];
};

export type AdminNavigationGroup = {
  label: string;
  items: AdminNavigationItem[];
};

export const adminDashboardPermissions = [
  "catalog.manage",
  "quotes.read",
  "quotes.manage",
  "contacts.read",
  "contacts.manage",
  "imports.manage",
  "imports.publish",
  "media.manage",
  "users.manage",
  "content.manage",
  "settings.manage"
] as const;

const navigation: AdminNavigationGroup[] = [
  {
    label: "SEO",
    items: [
      {
        label: "SEO Yönetimi",
        href: "/admin/seo",
        description: "Metadata ve arama görünürlüğü",
        anyPermission: ["catalog.manage", "content.manage"]
      }
    ]
  },
  {
    label: "Genel",
    items: [
      {
        label: "Dashboard",
        href: "/admin",
        description: "Operasyon özeti",
        anyPermission: adminDashboardPermissions
      },
      {
        label: "Site Ayarları",
        href: "/admin/settings",
        description: "İletişim ve kurumsal bilgiler",
        permission: "settings.manage"
      }
    ]
  },
  {
    label: "Katalog Yönetimi",
    items: [
      {
        label: "Ürünler",
        href: "/admin/catalog/products",
        description: "Ürün ve yayın yönetimi",
        permission: "catalog.manage"
      },
      {
        label: "Kategoriler",
        href: "/admin/catalog/categories",
        description: "Katalog sınıflandırması",
        permission: "catalog.manage"
      },
      {
        label: "Markalar",
        href: "/admin/catalog/brands",
        description: "Marka kayıtları",
        permission: "catalog.manage"
      },
      {
        label: "Varyantlar",
        href: "/admin/catalog/variants",
        description: "Ürün seçenekleri",
        permission: "catalog.manage"
      },
      {
        label: "Özellikler",
        href: "/admin/catalog/attributes",
        description: "Teknik özellik tanımları",
        permission: "catalog.manage"
      },
      {
        label: "Etiketler ve Birimler",
        href: "/admin/catalog/dictionaries",
        description: "Katalog sözlükleri",
        permission: "catalog.manage"
      },
      {
        label: "Ürün İlişkileri",
        href: "/admin/catalog/relations",
        description: "Benzer ve tamamlayıcı ürünler",
        permission: "catalog.manage"
      }
    ]
  },
  {
    label: "İçerik Yönetimi",
    items: [
      {
        label: "Kurumsal sayfalar",
        href: "/admin/content/pages",
        description: "Hakkımızda, hizmetler ve referanslar",
        permission: "content.manage"
      },
      {
        label: "Menü yönetimi",
        href: "/admin/content/menu",
        description: "Header ve footer bağlantıları",
        permission: "content.manage"
      },
      {
        label: "Ana sayfa bannerları",
        href: "/admin/content/heroes",
        description: "Hero ve CTA alanları",
        permission: "content.manage"
      },
      {
        label: "Galeri",
        href: "/admin/content/gallery",
        description: "Kurumsal galeri",
        permission: "content.manage"
      },
      {
        label: "Basın Odası",
        href: "/admin/content/press",
        description: "Basın duyuruları",
        permission: "content.manage"
      }
    ]
  },
  {
    label: "Operasyon",
    items: [
      {
        label: "Medya Kütüphanesi",
        href: "/admin/media",
        description: "Dosya ve görseller",
        permission: "media.manage"
      },
      {
        label: "Teklif Talepleri",
        href: "/admin/quotes",
        description: "Teklif operasyonu",
        anyPermission: ["quotes.read", "quotes.manage"]
      },
      {
        label: "İletişim Talepleri",
        href: "/admin/contact",
        description: "İletişim formu kayıtları",
        anyPermission: ["contacts.read", "contacts.manage"]
      },
      {
        label: "Müşteri Şikâyetleri",
        href: "/admin/contact/complaints",
        description: "Müşteri şikâyeti kayıtları",
        anyPermission: ["contacts.read", "contacts.manage"]
      },
      {
        label: "Kullanıcılar ve Yetkiler",
        href: "/admin/users",
        description: "Admin hesabı ve izinleri",
        permission: "users.manage"
      },
      {
        label: "Veri İçe Aktarma",
        href: "/admin/imports",
        description: "Toplu veri operasyonları",
        permission: "imports.manage"
      }
    ]
  }
];

export function navigationForPermissions(
  permissions: readonly string[]
): AdminNavigationGroup[] {
  const granted = new Set(permissions);
  return navigation
    .map((group) => ({
      ...group,
      items: group.items.filter(
        (item) =>
          (!item.permission || granted.has(item.permission)) &&
          (!item.anyPermission ||
            item.anyPermission.some((permission) => granted.has(permission)))
      )
    }))
    .filter((group) => group.items.length > 0);
}

export function canAccessAdminPath(
  pathname: string,
  permissions: readonly string[]
): boolean {
  if (pathname === "/admin/login") return true;

  const granted = new Set(permissions);
  if (pathname === "/admin") {
    return adminDashboardPermissions.some((permission) =>
      granted.has(permission)
    );
  }

  const routePermissions: ReadonlyArray<{
    prefix: string;
    permissions: readonly string[];
  }> = [
    { prefix: "/admin/catalog", permissions: ["catalog.manage"] },
    { prefix: "/admin/content", permissions: ["content.manage"] },
    { prefix: "/admin/contact", permissions: ["contacts.read", "contacts.manage"] },
    { prefix: "/admin/media", permissions: ["media.manage"] },
    { prefix: "/admin/quotes", permissions: ["quotes.read", "quotes.manage"] },
    { prefix: "/admin/imports", permissions: ["imports.manage"] },
    { prefix: "/admin/users", permissions: ["users.manage"] },
    { prefix: "/admin/seo", permissions: ["catalog.manage", "content.manage"] },
    { prefix: "/admin/settings", permissions: ["settings.manage"] }
  ];
  const rule = routePermissions.find(
    (item) => pathname === item.prefix || pathname.startsWith(`${item.prefix}/`)
  );

  return rule
    ? rule.permissions.some((permission) => granted.has(permission))
    : adminDashboardPermissions.some((permission) => granted.has(permission));
}

export function isAdminNavigationItemActive(
  pathname: string,
  href: string
): boolean {
  return href === "/admin"
    ? pathname === href
    : href === "/admin/contact"
      ? pathname === href
    : pathname === href || pathname.startsWith(`${href}/`);
}

export function findAdminNavigationItem(
  pathname: string,
  groups: readonly AdminNavigationGroup[]
): AdminNavigationItem | undefined {
  return groups
    .flatMap((group) => group.items)
    .filter((item) => isAdminNavigationItemActive(pathname, item.href))
    .sort((left, right) => right.href.length - left.href.length)[0];
}
