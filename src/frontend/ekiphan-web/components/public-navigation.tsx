import Link from "next/link";

import { HoverProductsMenu } from "./hover-products-menu";
import { HeaderSearch } from "./header-search";
import { ProductsMegaMenu } from "./products-mega-menu";

import styles from "./public-navigation.module.css";

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

export function PublicNavigation({
  currentPath
}: PublicNavigationProps) {
  const activePath =
    currentPath?.split("#", 1)[0] ?? "/";
  const isEnglish = currentPath?.startsWith("/en") ?? false;

  return (
    <nav
      id="public-mobile-navigation"
      className={styles.mainNav}
      aria-label="Ana menü"
    >
      {navigationItems.map((item) => {
        const isActive =
          item.href === activePath;

        if (item.href === "/katalog") {
          return (
            <HoverProductsMenu
              className={`${styles.productsMenu} ${styles.productsMenuMega}`}
              key={item.href}
            >
              <summary
                aria-label="Ürün kategorilerini göster"
              >
                <Link
                  className={`${styles.navItem}${
                    isActive
                      ? ` ${styles.active}`
                      : ""
                  }`}
                  href={item.href}
                  aria-current={isActive ? "page" : undefined}
                >
                  {item.label}
                </Link>
              </summary>

              <ProductsMegaMenu />
            </HoverProductsMenu>
          );
        }

        if (item.href === "/galeri") {
          const isGalleryActive =
            isActive ||
            activePath === "/showroom";

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
                {item.label}
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
                      href="/galeri"
                    >
                      GALERİ
                    </Link>

                    <p>
                      Projelerimizi ve ürünlerimizi
                      keşfedin.
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
                      href="/galeri?category=Showroom"
                    >
                      SHOWROOM
                    </Link>

                    <p>
                      Showroom alanımızı keşfedin.
                    </p>
                  </div>
                </div>

                <Link
                  className={
                    styles.productsMegaAll
                  }
                  href="/galeri"
                >
                  Galeriyi Keşfet{" "}
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
            href={item.href}
            aria-current={
              isActive
                ? "page"
                : undefined
            }
            key={item.href}
          >
            {item.label}
          </Link>
        );
      })}
      <div className={styles.mobileNavUtilities}>
        <HeaderSearch />
        <div>
          <Link href="/teklif-listem">TEKLİF AL</Link>
          <Link href={isEnglish ? "/" : "/en"}>{isEnglish ? "TR" : "EN"}</Link>
        </div>
      </div>
    </nav>
  );
}
