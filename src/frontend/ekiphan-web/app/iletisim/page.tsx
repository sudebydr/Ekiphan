import type { Metadata } from "next";
import { PublicHeader } from "../../components/public-header";
import { ContactForm } from "./contact-form";
import styles from "./contact.module.css";

export const dynamic = "force-dynamic";
export const metadata: Metadata = { title: "\u0130leti\u015fim", description: "Ekiphan ile \u00fcr\u00fcn, proje ve i\u015f birli\u011fi talepleriniz i\u00e7in ileti\u015fime ge\u00e7in.", alternates: { canonical: "/iletisim" } };

const contactCards: Array<{ icon: string; title: string; text: string; link?: { href: string; label: string } }> = [
  { icon: "\u25cf", title: "Fabrika ve Merkez", text: "Serik Cd. Sinan Mh. Havaalan\u0131 Yolu 10. km No: 107, 07170 Alt\u0131nova / Kepez / ANTALYA" },
  { icon: "\u260e", title: "Telefon", text: "\u00dcr\u00fcn, sipari\u015f ve genel bilgi talepleriniz i\u00e7in bizi aray\u0131n.", link: { href: "tel:+902423402515", label: "0242 340 25 15" } },];

export default function ContactPage() {
  return <main className={styles.page} data-public-page>
    <PublicHeader currentPath="/iletisim" />
    <section className={styles.hero} aria-labelledby="contact-page-title"><div className={styles.heroCopy}>
      <nav className={styles.breadcrumb} aria-label="Sayfa yolu"><a href="/">Ana Sayfa</a><span>{"\u203a"}</span><strong>{"\u0130leti\u015fim"}</strong></nav>
      <p>{"EK\u0130PHAN \u00b7 \u0130LET\u0130\u015e\u0130M"}</p><h1 id="contact-page-title">{"Projenizi birlikte konu\u015fal\u0131m."}</h1><span>{"\u00dcr\u00fcnlerimiz, projelerimiz ve i\u015f birlikleri hakk\u0131nda t\u00fcm talepleriniz i\u00e7in bizimle ileti\u015fime ge\u00e7ebilirsiniz."}</span>
    </div></section>
    <div className={styles.layout}>
      <section className={styles.formPanel} id="contact-form" aria-labelledby="contact-form-title"><div className={styles.formHeading}><p className={styles.sectionLabel}>{"\u2709 \u00a0 MESAJ G\u00d6NDER\u0130N"}</p><h2 id="contact-form-title">{"Size nas\u0131l yard\u0131mc\u0131 olabiliriz?"}</h2></div><ContactForm /></section>
      <aside className={styles.infoCards} aria-label="\u0130leti\u015fim bilgileri">{contactCards.map((card) => <article key={card.title} className={styles.infoCard}><span className={styles.infoIcon} aria-hidden="true">{card.icon}</span><div><h2>{card.title}</h2><p>{card.text}</p>{card.link && <a href={card.link.href}>{card.link.label}</a>}</div>{card.title === "Fabrika ve Merkez" && <iframe className={styles.locationMap} title="Ekiphan fabrika ve merkez konumu" src="https://www.google.com/maps?q=Serik%20Cd.%20Sinan%20Mh.%20Havaalan%C4%B1%20Yolu%2010.%20km%20No%3A%20107%2C%2007170%20Alt%C4%B1nova%20%2F%20Kepez%20%2F%20Antalya&output=embed" loading="lazy" referrerPolicy="no-referrer-when-downgrade" />}</article>)}</aside>
    </div>
  </main>;
}