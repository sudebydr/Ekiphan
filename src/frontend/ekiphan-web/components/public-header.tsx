import Link from "next/link";
import { HeaderSearch } from "./header-search";
import { PublicNavigation } from "./public-navigation";
import styles from "./public-header.module.css";

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

export function PublicHeader({ currentPath, tone = "light" }: PublicHeaderProps) {
  const isEnglish = currentPath?.startsWith("/en") ?? false;

  return (
    <header className={`${styles.catalogTopbar}${(currentPath === "/" || currentPath === "/en") ? ` ${styles.homeTopbar}` : currentPath === "/showroom" ? ` ${styles.showroomTopbar}` : ""}`}>
      <div className={styles.headerTop}>
        <Link className={styles.catalogBrand} href="/" aria-label="Ekiphan ana sayfa">
          <strong>ekiphan<span aria-hidden="true" /></strong>
          <small>PROFESYONEL MUTFAK ÇÖZÜMLERİ</small>
        </Link>
        <HeaderSearch />
        <div className={styles.headerActions}>
          <Link className={styles.quoteButton} href="/teklif-listem">Teklif Al</Link>
          <Link className={styles.headerLanguage} href={isEnglish ? "/" : "/en"} aria-label={isEnglish ? "Türkçeye geç" : "Switch to English"}>{isEnglish ? "TR" : "EN"}</Link>
        </div>
      </div>
      <PublicNavigation currentPath={currentPath} tone={tone} fallback={fallbackNavigation} />
    </header>
  );
}