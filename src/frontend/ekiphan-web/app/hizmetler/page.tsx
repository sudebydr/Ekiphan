import type { Metadata } from "next";
import { PublicHeader } from "../../components/public-header";
import { ServicesSection } from "../../components/services-section";
import styles from "./services.module.css";

export const metadata: Metadata = {
  title: "Hizmetlerimiz",
  description: "Ekiphan proje dan\u0131\u015fmanl\u0131\u011f\u0131, profesyonel mutfak planlama, kurulum, teknik servis ve yedek par\u00e7a hizmetleri."
};

export default function ServicesPage() {
  return <main
    className={styles.page}
    data-public-page
    style={{ display: "block", width: "100vw", maxWidth: "none", margin: 0, padding: 0 }}
  >
    <PublicHeader currentPath="/hizmetler" />
    <section className={styles.hero} aria-labelledby="services-page-title">
      <div className={styles.heroInner}>
        <div className={styles.heroCopy}>
          <nav className={styles.breadcrumb} aria-label="Breadcrumb"><a href="/">Ana Sayfa</a><span>&gt;</span><strong>Hizmetler</strong></nav>
          <h1 id="services-page-title">{"Hizmetlerimizle yan\u0131n\u0131zday\u0131z."}</h1>
          <i className={styles.accent} aria-hidden="true" />
          <p>{"Profesyonel mutfak projelerinde planlama, \u00fcr\u00fcn se\u00e7imi, kurulum ve sat\u0131\u015f sonras\u0131 deste\u011fi tek \u00e7at\u0131 alt\u0131nda sunuyoruz."}</p>
        </div>
        <div className={styles.heroVisual}><img src="/images/feature-consultation.png" alt="Profesyonel mutfak projesi i\u00e7in planlama \u00e7izimi" width={1200} height={900} /></div>
      </div>
    </section>
    <ServicesSection />
  </main>;
}