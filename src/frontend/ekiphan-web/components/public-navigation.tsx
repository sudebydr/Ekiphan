import Link from "next/link";

import { HoverProductsMenu } from "./hover-products-menu";
import { HeaderSearch } from "./header-search";
import { ProductsMegaMenu } from "./products-mega-menu";

import styles from "./public-navigation.module.css";
import { localeFromPath, localizedPath, publicCopy } from "../lib/public-locale";

export type NavigationFallback = {
  label: string;
  url: string;
};

type PublicNavigationProps = {
  currentPath?: string;
  fallback: NavigationFallback[];
  tone?: "default" | "dark" | "light";
};

const navigationItems = [
  { label: "ANA SAYFA", href: "/" },
  { label: "ÜRÜNLER", href: "/katalog" },
  { label: "HİZMETLER", href: "/hizmetler" },
  { label: "HAKKIMIZDA", href: "/hakkimizda" },
  { label: "ENDÜSTRİYEL MUTFAK", href: "/endustriyel-mutfak" },
  { label: "REFERANSLAR", href: "/referanslar" },
  { label: "GALERİ", href: "/galeri" },
  { label: "KATALOGLAR", href: "/kataloglar" },
  { label: "İLETİŞİM", href: "/iletisim" }
] as const;
const englishLabels = ["HOME", "PRODUCTS", "SERVICES", "ABOUT US", "INDUSTRIAL KITCHEN", "REFERENCES", "GALLERY", "CATALOGS", "CONTACT"] as const;

export function PublicNavigation({
  currentPath
}: PublicNavigationProps) {
  const activePath =
    currentPath?.split("#", 1)[0] ?? "/";
  const locale = localeFromPath(currentPath);
  const isEnglish = locale === "en";

  return (
    <nav
      id="public-mobile-navigation"
      className={styles.mainNav}
      aria-label={isEnglish ? "Main menu" : "Ana menü"}
    >
      {navigationItems.map((item) => {
        const destination = localizedPath(item.href, locale);
        const isActive = item.href === activePath || destination === activePath;
          const label = isEnglish ? englishLabels[navigationItems.indexOf(item)] : item.label;

        if (item.href === "/katalog") {
          return (
            <HoverProductsMenu
              className={`${styles.productsMenu} ${styles.productsMenuMega}`}
              key={item.href}
            >
              <summary
                aria-label={isEnglish ? "Show product categories" : "Ürün kategorilerini göster"}
              >
                <Link
                  className={`${styles.navItem}${
                    isActive
                      ? ` ${styles.active}`
                      : ""
                  }`}
                  href={destination}
                  aria-current={isActive ? "page" : undefined}
                >
            {label}
                </Link>
              </summary>

              <ProductsMegaMenu locale={locale} />
            </HoverProductsMenu>
          );
        }

        if (item.href === "/galeri") {
          const isGalleryActive =
            isActive ||
            activePath === "/showroom" ||
            activePath === "/en/showroom";

          return (
            <HoverProductsMenu
              className={`${styles.productsMenu} ${styles.productsMenuMega} ${styles.galleryMenu}`}
              key={item.href}
            >
              <summary
                className={`${styles.navItem}${
                  isGalleryActive
                    ? ` ${styles.active}`
                    : ""
                }`}
              >
                  {label}
              </summary>

              <div
                className={`${styles.productsSubmenu} ${styles.productsMegaPanel}`}
              >
                <div
                  className={styles.productsMegaGrid}
                >
                  <div
                    className={
                      styles.productsMegaColumn
                    }
                  >
                    <Link
                      className={
                        styles.productsMegaHeading
                      }
                      href={localizedPath("/galeri", locale)}
                    >
                      {isEnglish ? "GALLERY" : "GALERİ"}
                    </Link>

                    <p>
                      {isEnglish ? "Explore our projects and products." : "Projelerimizi ve ürünlerimizi keşfedin."}
                    </p>
                  </div>

                  <div
                    className={
                      styles.productsMegaColumn
                    }
                  >
                    <Link
                      className={
                        styles.productsMegaHeading
                      }
                      href={localizedPath("/galeri?category=Showroom", locale)}
                    >
                      SHOWROOM
                    </Link>

                    <p>
                      {isEnglish ? "Explore our showroom." : "Showroom alanımızı keşfedin."}
                    </p>
                  </div>
                </div>

                <Link
                  className={
                    styles.productsMegaAll
                  }
                  href={localizedPath("/galeri", locale)}
                >
                  {isEnglish ? "Explore the Gallery" : "Galeriyi Keşfet"}{" "}
                  <span aria-hidden="true">
                    →
                  </span>
                </Link>
              </div>
            </HoverProductsMenu>
          );
        }

        return (
          <Link
            className={`${styles.navItem}${
              isActive
                ? ` ${styles.active}`
                : ""
            }`}
            href={destination}
            aria-current={
              isActive
                ? "page"
                : undefined
            }
            key={item.href}
          >
            {label}
          </Link>
        );
      })}
      <div className={styles.mobileNavUtilities}>
        <HeaderSearch locale={locale} />
        <div>
          <Link href={localizedPath("/teklif-listem", locale)}>{isEnglish ? "REQUEST A QUOTE" : "TEKLİF AL"}</Link>
          <Link href={localizedPath(activePath, isEnglish ? "tr" : "en")} hrefLang={isEnglish ? "tr" : "en"}>{isEnglish ? "TR" : "EN"}</Link>
        </div>
      </div>
    </nav>
  );
}
