import Link from "next/link";

import { HeaderSearch } from "./header-search";
import MobileNavToggle from "./mobile-nav-toggle";
import { PublicNavigation } from "./public-navigation";
import { ScrollNavbar } from "./scroll-navbar";
import styles from "./public-header.module.css";
import { localeFromPath, localizedPath, publicCopy } from "../lib/public-locale";

type PublicHeaderProps = {
  currentPath?: string;
  tone?: "default" | "dark" | "light";
};

const fallbackNavigation = [
  { label: "Ana Sayfa", url: "/" },
  { label: "Ürünler", url: "/katalog" },
  { label: "Hizmetler", url: "/hizmetler" },
  { label: "Hakkımızda", url: "/hakkimizda" },
  { label: "Referanslar", url: "/referanslar" },
  { label: "Galeri", url: "/galeri" },
  { label: "Basın Odası", url: "/basin-odasi" },
  { label: "İletişim", url: "/iletisim" }
];

export function PublicHeader({
  currentPath,
  tone = "light"
}: PublicHeaderProps) {
  const locale = localeFromPath(currentPath);
  const languagePath = localizedPath(currentPath ?? "/", locale === "en" ? "tr" : "en");
  return (
    <header
      data-sticky-nav="true"
      className={`${styles.catalogTopbar}${
        currentPath === "/" || currentPath === "/en"
          ? ` ${styles.homeTopbar}`
          : currentPath === "/showroom"
            ? ` ${styles.showroomTopbar}`
            : ""
      }`}
    >
      <ScrollNavbar />
      <div className={styles.headerTop}>
        <Link
          className={styles.catalogBrand}
          href={locale === "en" ? "/en" : "/"}
          aria-label={locale === "en" ? "Ekiphan home page" : "Ekiphan ana sayfa"}
        >
          <strong>
            ekiphan<span aria-hidden="true" />
          </strong>

          <small>
            {locale === "en" ? "PROFESSIONAL KITCHEN SOLUTIONS" : "PROFESYONEL MUTFAK ÇÖZÜMLERİ"}
          </small>
        </Link>

        <HeaderSearch locale={locale} />

        <div className={styles.headerActions}>
          <Link
            className={styles.headerLanguage}
            href={languagePath}
            aria-label={locale === "en" ? "Switch to Turkish" : "Switch to English"}
            hrefLang={locale === "en" ? "tr" : "en"}
          >
            {locale === "en" ? "TR" : "EN"}
          </Link>
          <Link
            className={styles.quoteButton}
            href={localizedPath("/teklif-listem", locale)}
          >
            {publicCopy[locale].quote}
          </Link>

        </div>

        <MobileNavToggle />
      </div>

      <PublicNavigation
        currentPath={currentPath}
        tone={tone}
        fallback={fallbackNavigation}
      />
    </header>
  );
}
