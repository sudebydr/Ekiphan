"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import styles from "./public-footer.module.css";
import type { SiteSettingsDto } from "../lib/public-settings";

const groups = [
  { title: "Kurumsal", links: [{ href: "/hakkimizda", label: "Biz Kimiz" }, { href: "/referanslar", label: "Referanslar" }, { href: "/galeri", label: "Galeri" }, { href: "/showroom", label: "Showroom" }, { href: "/basin-odasi", label: "Basın Odası" }] },
  { title: "Ürünler", links: [{ href: "/urunler", label: "Tüm Ürünler" }, { href: "/endustriyel-mutfak", label: "Endüstriyel Mutfak" }] }
];

export function PublicFooter({ settings }: { settings: SiteSettingsDto | null }) {
  const pathname = usePathname();
  if (pathname.startsWith("/admin")) return null;

  const phone = settings?.phone || "0242 340 25 15";
  const address = settings?.showroomAddress || "Serik Cd. Sinan Mh. Havaalanı Yolu 10. km No: 107, 07170 Altınova / Kepez / ANTALYA";
  const email = settings?.contactEmail || "info@ekiphan.com.tr";

  return (
    <footer className={styles.footer}>
      <div className={styles.inner}>
        <section className={styles.brandColumn} aria-label="Ekiphan hakkında">
          <Link className={styles.brand} href="/" aria-label="Ekiphan ana sayfa">EKİPHAN<span>.</span></Link>
          <p>{settings?.companySlogan || "Profesyonel mutfak ve otel ekipmanlarında güçlü çözüm ortağınız."}</p>
          <div className={styles.socials} aria-label="Sosyal medya">
            {settings?.instagramUrl && <a href={settings.instagramUrl} aria-label="Instagram" target="_blank" rel="noopener noreferrer">ig</a>}
            {settings?.linkedInUrl && <a href={settings.linkedInUrl} aria-label="LinkedIn" target="_blank" rel="noopener noreferrer">in</a>}
            {settings?.youTubeUrl && <a href={settings.youTubeUrl} aria-label="YouTube" target="_blank" rel="noopener noreferrer">▶</a>}
          </div>
        </section>

        {groups.map((group) => (
          <nav className={styles.linkGroup} aria-label={group.title} key={group.title}>
            <h2>{group.title}</h2>
            {group.links.map((link) => <Link href={link.href} key={link.href}>{link.label}</Link>)}
          </nav>
        ))}

        <section className={styles.contactColumn} aria-label="İletişim">
          <h2>İletişim</h2>
          <a href={`tel:${phone.replace(/\s+/g, "")}`}>☎&nbsp; {phone}</a>
          <p>{address}</p>
          <a href={`mailto:${email}`}>{email}</a>
        </section>

        <div className={styles.bottom}>
          <span>© {new Date().getFullYear()} {settings?.companyTitle || "Ekiphan"}. Tüm hakları saklıdır.</span>
          <a className={styles.mostIdea} href="https://www.mostidea.com.tr/" target="_blank" rel="noopener noreferrer">DIGITAL EXPERIENCE BY MOST IDEA</a>
          <nav aria-label="Yasal bağlantılar"><Link href="/gizlilik">Gizlilik</Link><Link href="/cerezler">Çerezler</Link><Link href="/kvkk">KVKK</Link></nav>
        </div>
      </div>
    </footer>
  );
}
