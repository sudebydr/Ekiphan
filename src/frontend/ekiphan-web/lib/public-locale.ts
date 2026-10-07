export type PublicLocale = "tr" | "en";

const trToEn: Record<string, string> = {
  "/": "/en",
  "/hakkimizda": "/en/about",
  "/hizmetler": "/en/services",
  "/katalog": "/en/products",
  "/referanslar": "/en/references",
  "/galeri": "/en/gallery",
  "/basin-odasi": "/en/press",
  "/iletisim": "/en/contact",
  "/teklif-listem": "/en/quote-list",
  "/kvkk": "/en/privacy",
  "/showroom": "/en/showroom"
};

const enToTr: Record<string, string> = Object.fromEntries(
  Object.entries(trToEn).map(([tr, en]) => [en, tr])
);

export function localeFromPath(pathname: string | undefined): PublicLocale {
  return pathname === "/en" || pathname?.startsWith("/en/") ? "en" : "tr";
}

export function localizedPath(pathname: string, targetLocale: PublicLocale): string {
  const [path, query = ""] = pathname.split("?", 2);
  const map = targetLocale === "en" ? trToEn : enToTr;
  const exact = map[path];
  if (exact) return `${exact}${query ? `?${query}` : ""}`;

  if (targetLocale === "en" && path.startsWith("/katalog/")) {
    return `/en/products/${path.slice("/katalog/".length)}${query ? `?${query}` : ""}`;
  }
  if (targetLocale === "tr" && path.startsWith("/en/products/")) {
    return `/katalog/${path.slice("/en/products/".length)}${query ? `?${query}` : ""}`;
  }

  return targetLocale === "en" ? "/en" : "/";
}

export const publicCopy = {
  tr: {
    home: "Ana Sayfa",
    about: "Hakkımızda",
    services: "Hizmetler",
    products: "Ürünler",
    references: "Referanslar",
    gallery: "Galeri",
    press: "Basın Odası",
    contact: "İletişim",
    quote: "Teklif Al",
    search: "Katalogda ara"
  },
  en: {
    home: "Home",
    about: "About",
    services: "Services",
    products: "Products",
    references: "References",
    gallery: "Gallery",
    press: "Press Room",
    contact: "Contact",
    quote: "Request a Quote",
    search: "Search catalog"
  }
} as const;