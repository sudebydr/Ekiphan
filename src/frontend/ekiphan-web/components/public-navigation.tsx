import Link from "next/link";
import { HoverProductsMenu } from "./hover-products-menu";
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

export function PublicNavigation({ currentPath }: PublicNavigationProps) {
  const activePath = currentPath?.split("#", 1)[0] ?? "/";

  return (
    <nav className={styles.mainNav} aria-label="Ana menü">
      {navigationItems.map((item) => {
        const isActive = item.href === activePath;
        if (item.href === "/katalog") {
          return (
            <HoverProductsMenu className={`${styles.productsMenu} ${styles.productsMenuMega}`} key={item.href}>
              <summary className={`${styles.navItem}${isActive ? ` ${styles.active}` : ""}`}>{item.label}</summary>
              <ProductsMegaMenu />
            </HoverProductsMenu>
          );
        }
        if (item.href === "/galeri") {
          const isGaleriGroupActive = isActive || activePath === "/showroom";
          return (
            <HoverProductsMenu className={styles.productsMenu} key={item.href}>
              <summary className={`${styles.navItem}${isGaleriGroupActive ? ` ${styles.active}` : ""}`}>{item.label}</summary>
              <div className={styles.productsSubmenu}>
                <Link href="/galeri">Galeri</Link>
                <Link href="/galeri?category=Showroom">Showroom</Link>
              </div>
            </HoverProductsMenu>
          );
        }
        return <Link className={`${styles.navItem}${isActive ? ` ${styles.active}` : ""}`} href={item.href} aria-current={isActive ? "page" : undefined} key={item.href}>{item.label}</Link>;
      })}
    </nav>
  );
}
