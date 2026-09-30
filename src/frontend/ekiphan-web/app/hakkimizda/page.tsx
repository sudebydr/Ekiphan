import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../../components/public-header";
import styles from "./about.module.css";

export const metadata: Metadata = {
  title: "Hakkımızda",
  description:
    "Ekiphan'ın profesyonel mutfak çözümlerine yaklaşımı, değerleri ve çalışma ilkeleri.",
};

const values = [
  [
    "01",
    "Güvenilirlik",
    "Sürecin her aşamasında açık iletişim ve tutarlı teslim.",
  ],
  [
    "02",
    "Uzmanlık",
    "Sektör bilgisini projenizin ihtiyaçlarıyla birleştiririz.",
  ],
  [
    "03",
    "Çözüm Odaklılık",
    "Her alan için uygulanabilir, verimli bir yol haritası kurarız.",
  ],
  [
    "04",
    "Sürdürülebilir İş Birliği",
    "Teslimin ötesinde, uzun soluklu destek sunarız.",
  ],
] as const;

export default function AboutPage() {
  return (
    <main className={styles.page} data-public-page>
      <PublicHeader currentPath="/hakkimizda" />

      {/* HERO */}
      <section className={styles.hero}>
        <div className={styles.heroImage} aria-hidden="true" />

        <div className={styles.heroContent}>
          <p className={styles.eyebrow}>EKİPHAN’I TANIYIN</p>

          <h1>
            Köklerimizden
            <br />
            <em>Geleceğe</em>
          </h1>

          <div className={styles.heroLine} />

          <p className={styles.heroText}>
            Ekiphan; profesyonel mutfakların ihtiyaçlarını dinleyen, doğru
            ekipmanı doğru projeyle buluşturan bir çözüm ortağıdır.
          </p>

          <p className={styles.heroText}>
            Planlamadan tedarike, kurulumdan satış sonrası desteğe kadar her
            adımda deneyimimizi işletmenizin ritmiyle birleştiriyoruz.
          </p>
        </div>

        <div className={styles.heroNumber}>01</div>
      </section>

      {/* BİZ KİMİZ */}
      <section className={styles.identity}>
        <div className={styles.sectionIndex}>02</div>

        <div className={styles.sectionLabel}>
          <span>BİZ KİMİZ?</span>
        </div>

        <div className={styles.identityContent}>
          <h2>
            İhtiyacı anlayan,
            <br />
            çözümü birlikte
            <br />
            <em>tasarlayan ekip.</em>
          </h2>

          <div className={styles.identityCopy}>
            <p>
              Otel, restoran, kafe ve endüstriyel mutfak projelerinde güçlü
              marka seçkisini operasyonel bilgiyle bir araya getiriyoruz.
            </p>

            <p>
              Her projenin kendine özgü kullanım alışkanlığını, kapasitesini
              ve hedefini dikkate alıyoruz.
            </p>

            <p>
              Amacımız yalnızca ekipman tedarik etmek değil; uzun ömürlü,
              verimli ve güven veren çalışma alanları oluşturmaktır.
            </p>
          </div>
        </div>
      </section>

      {/* MİSYON / VİZYON */}
      <section className={styles.missionVision}>
        <div className={styles.sectionIndex}>03</div>

        <div className={styles.missionVisionHeader}>
          <span>YAKLAŞIMIMIZ</span>
          <h2>
            Bugünün ihtiyacını
            <br />
            yarının <em>değeriyle</em> buluşturuyoruz.
          </h2>
        </div>

        <div className={styles.missionVisionGrid}>
          <article>
            <span className={styles.cardNumber}>01</span>
            <p className={styles.eyebrow}>MİSYONUMUZ</p>

            <h3>
              İşletmeler için işlevsel,
              <br />
              güvenilir çözümler üretmek.
            </h3>

            <p className={styles.cardText}>
              Profesyonel ekipman bilgisini, ihtiyaca uygun planlama ve
              güvenilir uygulamayla buluştururuz.
            </p>
          </article>

          <article>
            <span className={styles.cardNumber}>02</span>
            <p className={styles.eyebrow}>VİZYONUMUZ</p>

            <h3>
              Her projede kalıcı değer
              <br />
              yaratan çözüm ortağı olmak.
            </h3>

            <p className={styles.cardText}>
              HoReCa dünyasında seçkin markalar ve güçlü hizmet anlayışıyla
              sürdürülebilir iş birlikleri kurarız.
            </p>
          </article>
        </div>
      </section>

      {/* DEĞERLER */}
      <section className={styles.values}>
        <div className={styles.sectionIndex}>04</div>

        <div className={styles.valuesHeader}>
          <p className={styles.eyebrow}>DEĞERLERİMİZ</p>

          <h2>
            Her projede aynı
            <br />
            çalışma <em>ilkeleri.</em>
          </h2>
        </div>

        <div className={styles.valuesGrid}>
          {values.map(([number, title, text]) => (
            <article key={number}>
              <span className={styles.valueNumber}>{number}</span>

              <div className={styles.valueLine} />

              <h3>{title}</h3>

              <p>{text}</p>
            </article>
          ))}
        </div>
      </section>

      {/* CTA */}
      <section className={styles.contactCta}>
        <img
          src="/images/showroom-experience.png"
          alt="Ekiphan showroom"
          loading="lazy"
        />

        <div className={styles.ctaOverlay} />

        <div className={styles.ctaContent}>
          <p className={styles.eyebrow}>BİRLİKTE PLANLAYALIM</p>

          <h2>
            İhtiyacınızı konuşalım,
            <br />
            doğru çözümü <em>birlikte kuralım.</em>
          </h2>

          <Link href="/iletisim#contact-form">
            İLETİŞİME GEÇİN
            <span>↗</span>
          </Link>
        </div>
      </section>
    </main>
  );
}