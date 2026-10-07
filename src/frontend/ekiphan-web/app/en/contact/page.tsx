import type { Metadata } from "next";
import { PublicHeader } from "../../../components/public-header";
import { ContactForm } from "../../iletisim/contact-form";
import styles from "../../iletisim/contact.module.css";

export const metadata: Metadata = { title: "Contact | Ekiphan", description: "Get in touch with Ekiphan about products, projects and partnerships.", alternates: { canonical: "/en/contact", languages: { tr: "/iletisim", en: "/en/contact" } } };

export default function EnglishContactPage() {
  return <main className={styles.page} data-public-page>
    <PublicHeader currentPath="/en/contact" />
    <section className={styles.hero} aria-labelledby="contact-page-title"><div className={styles.heroCopy}>
      <nav className={styles.breadcrumb} aria-label="Breadcrumb"><a href="/en">Home</a><span>›</span><strong>Contact</strong></nav>
      <p>EKIPHAN · CONTACT</p><h1 id="contact-page-title">Let’s discuss your project.</h1><span>Contact us with any enquiry about our products, projects and partnerships.</span>
    </div></section>
    <div className={styles.layout}>
      <section className={styles.formPanel} id="contact-form" aria-labelledby="contact-form-title"><div className={styles.formHeading}><p className={styles.sectionLabel}>✉ &nbsp; SEND A MESSAGE</p><h2 id="contact-form-title">How can we help you?</h2></div><ContactForm locale="en" /></section>
      <aside className={styles.infoCards} aria-label="Contact information">
        <article className={styles.infoCard}><span className={styles.infoIcon} aria-hidden="true">●</span><div><h2>Head Office & Factory</h2><p>Serik Cd. Sinan Mh. Havaalanı Yolu 10. km No: 107, 07170 Altınova / Kepez / ANTALYA</p><iframe className={styles.locationMap} title="Ekiphan head office and factory location" src="https://www.google.com/maps?q=Serik%20Cd.%20Sinan%20Mh.%20Havaalan%C4%B1%20Yolu%2010.%20km%20No%3A%20107%2C%2007170%20Alt%C4%B1nova%20%2F%20Kepez%20%2F%20Antalya&output=embed" loading="lazy" referrerPolicy="no-referrer-when-downgrade" /></div></article>
        <article className={styles.infoCard}><span className={styles.infoIcon} aria-hidden="true">☎</span><div><h2>Phone</h2><p>Call us for product, order and general enquiries.</p><a href="tel:+902423402515">0242 340 25 15</a></div></article>
      </aside>
    </div>
  </main>;
}
