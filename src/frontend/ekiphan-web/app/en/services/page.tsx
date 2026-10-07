import type { Metadata } from "next";
import { PublicHeader } from "../../../components/public-header";
import { ServicesSection } from "../../../components/services-section";
import styles from "../../hizmetler/services.module.css";

export const metadata: Metadata = {
  title: "Services | Ekiphan",
  description: "Project consultancy, kitchen planning, installation, technical service and spare parts from Ekiphan.",
  alternates: { canonical: "/en/services", languages: { tr: "/hizmetler", en: "/en/services" } }
};

export default function EnglishServicesPage() {
  return <main className={styles.page} data-public-page>
    <PublicHeader currentPath="/en/services" />
    <section className={styles.hero} aria-labelledby="services-page-title">
      <div className={styles.heroInner}>
        <div className={styles.heroCopy}>
          <nav className={styles.breadcrumb} aria-label="Breadcrumb"><a href="/en">Home</a><span aria-hidden="true">/</span><strong>Services</strong></nav>
          <h1 id="services-page-title">Here to support your project.</h1>
          <i className={styles.accent} aria-hidden="true" />
          <p>We bring planning, equipment selection, installation and after-sales support together for professional kitchen projects.</p>
        </div>
        <div className={styles.heroVisual}><img src="/images/feature-consultation.png" alt="Planning a professional kitchen project" width={1200} height={900} /></div>
      </div>
    </section>
    <ServicesSection locale="en" />
  </main>;
}
