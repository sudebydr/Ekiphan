import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../../components/public-header";
import styles from "./industrial.module.css";

export const metadata: Metadata = {
  title: "Endüstriyel Mutfak",
  description:
    "Profesyonel mutfak projeleri için ekipman ve uygulama çözümleri.",
  alternates: { canonical: "/endustriyel-mutfak" }
};

const categories = [
  [
    "Hazırlık Ekipmanları",
    "Çalışma tezgahları, evyeler ve hazırlık masaları",
    "hazirlik",
    "01"
  ],
  [
    "Pişirme Ekipmanları",
    "Ocaklar, fırınlar, ızgaralar ve pişirme üniteleri",
    "pisirme",
    "02"
  ],
  [
    "Soğutma Ekipmanları",
    "Buzdolapları, şok soğutucular ve soğuk odalar",
    "sogutma",
    "03"
  ],
  [
    "Yıkama Sistemleri",
    "Bulaşık makineleri, yıkama üniteleri ve armatürler",
    "yikama",
    "04"
  ],
  [
    "Saklama Sistemleri",
    "Duvar dolapları, çekmeceli dolaplar ve raf sistemleri",
    "saklama",
    "05"
  ],
  [
    "Havalandırma",
    "Davlumbazlar, baca sistemleri ve filtre çözümleri",
    "havalandirma",
    "06"
  ]
] as const;

const benefits = [
  [
    "01",
    "Proje Danışmanlığı",
    "Alan, kapasite ve operasyonunuza uygun planlama."
  ],
  [
    "02",
    "Doğru Ekipman",
    "Seçkin markalardan ihtiyacınıza uygun seçki."
  ],
  [
    "03",
    "Kurulum ve Devreye Alma",
    "Uygulama, montaj ve kullanıma alma desteği."
  ],
  [
    "04",
    "Satış Sonrası",
    "Bakım, teknik servis ve sürekli destek."
  ]
] as const;

export default function IndustrialKitchenPage() {
  return (
    <main className={styles.page} data-public-page>
      <PublicHeader currentPath="/endustriyel-mutfak" />

      <section className={styles.hero} aria-labelledby="industrial-title">
        <div className={styles.heroCopy}>
          <div className={styles.eyebrowRow}>
            <span>01</span>
            <i aria-hidden="true" />
            <p>ENDÜSTRİYEL MUTFAK</p>
          </div>

          <h1 id="industrial-title">
            Endüstriyel mutfaklarda <em>doğru çözüm ortağınız.</em>
          </h1>

          <p className={styles.heroLead}>
            Planlamadan ekipman seçimine, kurulumdan satış sonrası desteğe kadar
            profesyonel mutfak projelerinize uçtan uca değer katıyoruz.
          </p>

          <div className={styles.heroActions}>
            <Link href="/katalog" className={styles.primaryAction}>
              Ürünleri İncele <span aria-hidden="true">→</span>
            </Link>
            <Link href="/iletisim" className={styles.secondaryAction}>
              Projenizi Konuşalım <span aria-hidden="true">↗</span>
            </Link>
          </div>

          <div className={styles.heroMeta}>
            <span>PLANLAMA</span>
            <i aria-hidden="true" />
            <span>UYGULAMA</span>
            <i aria-hidden="true" />
            <span>DESTEK</span>
          </div>
        </div>

        <div className={styles.heroVisual} aria-hidden="true">
          <img
            src="/images/industrial-kitchen-premium.png"
            alt=""
            width={1536}
            height={1024}
            loading="eager"
          />
          <div className={styles.heroVisualShade} />
          <span className={styles.verticalLabel}>EKİPHAN</span>
          <span className={styles.heroSlide}>01 / 03</span>
        </div>
      </section>

      <section className={styles.intro} aria-labelledby="industrial-intro-title">
        <div className={styles.introIndex}>02</div>
        <div className={styles.introHeading}>
          <p className={styles.sectionKicker}>YAKLAŞIMIMIZ</p>
          <h2 id="industrial-intro-title">
            Mutfakları yalnızca <em>kurmuyoruz.</em>
            Operasyonunuzu düşünüyoruz.
          </h2>
        </div>
        <p className={styles.introCopy}>
          Doğru yerleşim, doğru ekipman ve doğru operasyon akışıyla işletmenize
          özel profesyonel mutfaklar oluşturuyoruz. İhtiyacı anlayıp seçimi,
          uygulamayı ve sonrasını aynı bütünün parçası olarak ele alıyoruz.
        </p>
      </section>

      <section className={styles.categories} aria-labelledby="category-title">
        <div className={styles.sectionTopline}>
          <div>
            <span className={styles.sectionNumber}>03</span>
            <p className={styles.sectionKicker}>EKİPMAN KATEGORİLERİ</p>
          </div>
          <p className={styles.sectionNote}>
            İhtiyacınıza göre şekillenen profesyonel ekipman dünyası.
          </p>
        </div>

        <h2 id="category-title" className={styles.sectionTitle}>
          Her istasyona doğru ekipman.
        </h2>

        <div className={styles.categoryGrid}>
          {categories.map(([title, text, key, number]) => (
            <Link
              key={key}
              href={`/katalog?section=mutfak&category=${key}`}
              className={styles.categoryCard}
            >
              <div className={`${styles.categoryPhoto} ${styles[key]}`}>
                <span>{number}</span>
              </div>
              <div className={styles.categoryBody}>
                <h3>{title}</h3>
                <p>{text}</p>
                <span className={styles.categoryLink}>
                  Kategoriyi incele <i aria-hidden="true">→</i>
                </span>
              </div>
            </Link>
          ))}
        </div>
      </section>

      <section className={styles.project} aria-labelledby="project-title">
        <div className={styles.projectImage}>
          <img
            src="/images/feature-kitchen-triptych-v1.png"
            alt=""
            width={1536}
            height={1024}
            loading="lazy"
          />
        </div>
        <div className={styles.projectCopy}>
          <div className={styles.projectIndex}>04</div>
          <p className={styles.sectionKicker}>PROJEYE ÖZEL YAKLAŞIM</p>
          <h2 id="project-title">
            Tasarımdan <em>devreye almaya</em> kadar tek ekip.
          </h2>
          <p>
            Alanınıza, kapasitenize ve operasyonunuza göre planlanan mutfaklar;
            seçilen ekipmanın doğru noktaya yerleşmesiyle gerçek değerine ulaşır.
          </p>
          <Link href="/iletisim" className={styles.projectAction}>
            Projenizi konuşalım <span aria-hidden="true">↗</span>
          </Link>
        </div>
      </section>

      <section className={styles.benefits} aria-labelledby="benefits-title">
        <div className={styles.benefitsHeading}>
          <span>05</span>
          <p className={styles.sectionKicker}>NEDEN EKİPHAN?</p>
          <h2 id="benefits-title">
            Güçlü proje, <em>doğru uygulama.</em>
          </h2>
        </div>

        <div className={styles.benefitGrid}>
          {benefits.map(([number, title, text]) => (
            <article key={number}>
              <span>{number}</span>
              <h3>{title}</h3>
              <p>{text}</p>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}
