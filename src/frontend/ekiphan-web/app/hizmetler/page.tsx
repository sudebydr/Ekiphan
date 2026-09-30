import type { Metadata } from "next";
import { PublicHeader } from "../../components/public-header";
import { ServicesSection } from "../../components/services-section";
import styles from "./services.module.css";

export const metadata: Metadata = {
  title: "Hizmetlerimiz | Ekiphan",
  description:
    "Ekiphan proje danışmanlığı, profesyonel mutfak planlama, kurulum, teknik servis ve yedek parça hizmetleri.",
};

export default function ServicesPage() {
  return (
    <main
      className={styles.page}
      data-public-page
    >
      <PublicHeader currentPath="/hizmetler" />

      <section
        className={styles.hero}
        aria-labelledby="services-page-title"
      >
        <div className={styles.heroInner}>
          <div className={styles.heroCopy}>
            <nav
              className={styles.breadcrumb}
              aria-label="Breadcrumb"
            >
              <a href="/">Ana Sayfa</a>

              <span aria-hidden="true">/</span>

              <strong>Hizmetler</strong>
            </nav>

            <h1 id="services-page-title">
              Hizmetlerimizle yanınızdayız.
            </h1>

            <i
              className={styles.accent}
              aria-hidden="true"
            />

            <p>
              Profesyonel mutfak projelerinde planlama,
              ürün seçimi, kurulum ve satış sonrası
              desteği tek çatı altında sunuyoruz.
            </p>
          </div>

          <div className={styles.heroVisual}>
            <img
              src="/images/feature-consultation.png"
              alt="Profesyonel mutfak projesi için planlama çizimi"
              width={1200}
              height={900}
            />
          </div>
        </div>
      </section>

      <ServicesSection />
    </main>
  );
}