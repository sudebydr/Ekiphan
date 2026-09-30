import type { Metadata } from "next";
import Image from "next/image";

import { PublicHeader } from "../../components/public-header";
import { getContentPage } from "../../lib/catalog-api";
import { managedMetadata } from "../../lib/managed-metadata";
import styles from "./references.module.css";

export const dynamic = "force-dynamic";

const references = [
  { name: "arcelik", accent: "red" },
  { name: "VESTEL", accent: "red" },
  { name: "divan", accent: "dark" },
  { name: "RAMADA", sub: "BY WYNDHAM", accent: "burgundy" },
  { name: "MEDA", sub: "TOWER", accent: "gray" },
  { name: "MEDICANA", accent: "teal" },
  { name: "DOĞUŞ", sub: "GRUBU", accent: "blue" },
  { name: "TAHA", sub: "KARGO", accent: "blue" },
  { name: "EGE", sub: "YAPI", accent: "gold" },
  { name: "Radisson", accent: "blue" },
  { name: "POLIMEKS", accent: "gray" },
  { name: "ATASAY", accent: "gold" },
  { name: "FERKO", accent: "teal" },
  { name: "akasya", accent: "orange" },
  { name: "İSKELE", sub: "YAPI", accent: "gray" }
];

export async function generateMetadata(): Promise<Metadata> {
  try {
    return managedMetadata(await getContentPage("referanslar"), "/referanslar");
  } catch {
    return {
      title: "Referanslar",
      robots: { index: false, follow: false }
    };
  }
}

export default function ReferencesPage() {
  return (
    <main className={styles.page} data-public-page>
      <PublicHeader currentPath="/referanslar" />

      <section className={styles.hero} aria-labelledby="references-title">
        <div className={styles.heroCopy}>
          <div className={styles.eyebrow}>
            <span>01</span>
            <i aria-hidden="true" />
            <strong>İŞ ORTAKLARIMIZ</strong>
          </div>

          <h1 id="references-title">
            Referanslarımız
            <em>birlikte ürettiklerimiz.</em>
          </h1>

          <p className={styles.heroLead}>
            Türkiye’nin ve dünyanın önde gelen markalarıyla,
            profesyonel mutfak projelerinde birlikte çalışıyoruz.
          </p>

          <div className={styles.heroMeta}>
            <span className={styles.heroMetaNumber}>15</span>
            <span className={styles.heroMetaLabel}>İŞ ORTAĞI</span>
          </div>
        </div>

        <div className={styles.heroVisual}>
          <Image
            src="/images/ekiphan-kitchen-hero.png"
            alt="Profesyonel mutfak projesi"
            fill
            priority
            sizes="(max-width: 900px) 100vw, 55vw"
          />
          <div className={styles.heroVisualShade} aria-hidden="true" />
          <span className={styles.heroVisualLabel}>EKİPHAN / REFERANSLAR</span>
        </div>
      </section>

      <section className={styles.brandSection} aria-labelledby="partners-title">
        <div className={styles.brandTopline}>
          <div className={styles.brandEyebrow}>
            <span>02</span>
            <span>GÜVENİLEN MARKALAR</span>
          </div>
          <div className={styles.partnerCount}>
            <strong>15+</strong>
            <span>İŞ ORTAĞI</span>
          </div>
        </div>

        <div className={styles.sectionIntro}>
          <div>
            <h2 id="partners-title">
              Güçlü iş ortaklıkları,
              <em>sağlam projeler.</em>
            </h2>
          </div>
          <p>
            Farklı sektörlerdeki projelerimizde birlikte çalıştığımız markaları
            aynı çatı altında buluşturuyoruz.
          </p>
        </div>

        <div className={styles.filters} aria-label="Referans kategorileri">
          <button type="button" className={styles.filterActive}>Tümü</button>
          <button type="button">Otel</button>
          <button type="button">Restoran</button>
          <button type="button">Endüstriyel Mutfak</button>
          <button type="button">Kafe</button>
          <button type="button">Kamu / Kurumsal</button>
          <button type="button">Yurt Dışı Projeler</button>
        </div>

        <div className={styles.brandGrid}>
          {references.map((brand, index) => (
            <article
              className={`${styles.brandCard} ${styles[`brand_${brand.accent}`]}`}
              key={brand.name}
            >
              <span className={styles.brandNumber}>
                {String(index + 1).padStart(2, "0")}
              </span>

              <div className={styles.brandMark}>
                <span>{brand.name}</span>
                {brand.sub ? <small>{brand.sub}</small> : null}
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className={styles.statement}>
        <span>03</span>
        <p>
          Her projede doğru marka, doğru ekipman ve doğru uygulamayı birlikte planlıyoruz.
        </p>
      </section>
    </main>
  );
}
