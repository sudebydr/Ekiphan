"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import styles from "./public-footer.module.css";
import type { SiteSettingsDto } from "../lib/public-settings";

const groups = [
  {
    title: "Hakkımızda",
    links: [
      { href: "/hakkimizda", label: "Biz Kimiz" },
      { href: "/referanslar", label: "Referanslar" },
      { href: "/galeri", label: "Galeri" },
      { href: "/basin-odasi", label: "Basın Odası" },
    ],
  },
  {
    title: "Kategoriler",
    links: [
      { href: "/katalog", label: "Tüm Ürünler" },
      { href: "/endustriyel-mutfak", label: "Endüstriyel Mutfak" },
      { href: "/kataloglar", label: "Kataloglar" },
      { href: "/showroom", label: "Showroom" },
    ],
  },
  {
    title: "Destek",
    links: [
      { href: "/iletisim", label: "İletişim" },
      { href: "/teklif-listem", label: "Teklif Al" },
    ],
  },
];

function SocialIcon({ network }: { network: "instagram" | "linkedin" | "youtube" }) {
  if (network === "instagram") {
    return (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <rect x="3.5" y="3.5" width="17" height="17" rx="5" />
        <circle cx="12" cy="12" r="4" />
        <circle className={styles.iconDot} cx="17.7" cy="6.7" r="0.8" />
      </svg>
    );
  }

  if (network === "linkedin") {
    return (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M5 9v10M5 5.5v.1M10 19v-6a4 4 0 0 1 8 0v6M10 10v9" />
      </svg>
    );
  }

  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M21 7.2a2.5 2.5 0 0 0-1.8-1.8C17.6 5 12 5 12 5s-5.6 0-7.2.4A2.5 2.5 0 0 0 3 7.2 26 26 0 0 0 2.6 12a26 26 0 0 0 .4 4.8 2.5 2.5 0 0 0 1.8 1.8C6.4 19 12 19 12 19s5.6 0 7.2-.4a2.5 2.5 0 0 0 1.8-1.8 26 26 0 0 0 .4-4.8 26 26 0 0 0-.4-4.8Z" />
      <path d="m10 9 5 3-5 3V9Z" />
    </svg>
  );
}

export function PublicFooter({ settings }: { settings: SiteSettingsDto | null }) {
  const pathname = usePathname();
  if (pathname.startsWith("/admin")) return null;

  const phone = settings?.phone || "0242 340 25 15";
  const address = settings?.showroomAddress || "Serik Cd. Sinan Mh. Havaalanı Yolu 10. km No: 107, 07170 Altınova / Kepez / ANTALYA";
  const email = settings?.contactEmail || "info@ekiphan.com.tr";
  const socialLinks = [
    { network: "instagram" as const, href: settings?.instagramUrl, label: "Instagram" },
    { network: "linkedin" as const, href: settings?.linkedInUrl, label: "LinkedIn" },
    { network: "youtube" as const, href: settings?.youTubeUrl, label: "YouTube" },
  ].filter((item): item is typeof item & { href: string } => Boolean(item.href));

  const scrollToTop = () => {
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    window.scrollTo({ top: 0, behavior: reducedMotion ? "auto" : "smooth" });
  };

  return (
    <footer className={styles.footer}>
      <div className={styles.inner}>
        <div className={styles.mainRow}>
          <section className={styles.brandColumn} aria-label="Ekiphan hakkında">
            <Link className={styles.brand} href="/" aria-label="Ekiphan ana sayfa">
              ekiphan<span aria-hidden="true">.</span>
            </Link>
            <p>{settings?.companySlogan || "Profesyonel mutfak ve otel ekipmanlarında güçlü çözüm ortağınız."}</p>
            {socialLinks.length > 0 && (
              <div className={styles.socials} aria-label="Sosyal medya">
                {socialLinks.map(({ network, href, label }) => (
                  <a key={network} href={href} aria-label={label} target="_blank" rel="noopener noreferrer">
                    <SocialIcon network={network} />
                  </a>
                ))}
              </div>
            )}
          </section>

          {groups.map((group) => (
            <nav className={styles.linkGroup} aria-label={group.title} key={group.title}>
              <h2>{group.title}</h2>
              {group.links.map((link) => <Link href={link.href} key={link.href}>{link.label}</Link>)}
            </nav>
          ))}

          <section className={styles.ctaColumn} aria-label="İletişim ve teklif">
            <p className={styles.eyebrow}>Projenizi birlikte planlayalım</p>
            <Link href="/teklif-listem" className={styles.primaryCta}>
              Teklif Al <span aria-hidden="true">↗</span>
            </Link>
            <Link href="/iletisim" className={styles.secondaryCta}>
              İletişim <span aria-hidden="true">→</span>
            </Link>
            <a className={styles.contactDetail} href={`tel:${phone.replace(/\s+/g, "")}`}>{phone}</a>
            <a className={styles.contactDetail} href={`mailto:${email}`}>{email}</a>
            <p className={styles.address}>{address}</p>
          </section>
        </div>

        <div className={styles.bottom}>
          <span>© {new Date().getFullYear()} {settings?.companyTitle || "Ekiphan"}. Tüm hakları saklıdır.</span>
          <a className={styles.mostIdea} href="https://www.mostidea.com.tr/" target="_blank" rel="noopener noreferrer">DIGITAL EXPERIENCE BY MOST IDEA</a>
          <nav aria-label="Yasal bağlantılar">
            <Link href="/gizlilik">Gizlilik</Link>
            <Link href="/cerezler">Çerezler</Link>
            <Link href="/kvkk">KVKK</Link>
          </nav>
          <button className={styles.backToTop} type="button" onClick={scrollToTop} aria-label="Sayfanın başına dön">
            <span aria-hidden="true">↑</span>
          </button>
        </div>
      </div>
    </footer>
  );
}
