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
const valuesEn = [
  ["01", "Reliability", "Clear communication and consistent delivery at every stage."],
  ["02", "Expertise", "We combine sector knowledge with the needs of your project."],
  ["03", "Solution Focus", "A practical, efficient roadmap for every space."],
  ["04", "Lasting Partnership", "Long-term support that continues beyond delivery."]
] as const;

export default function AboutPage({ locale = "tr" }: { locale?: "tr" | "en" } = {}) {
  const en = locale === "en";
  const displayedValues = en ? valuesEn : values;
  return (
    <main className={styles.page} data-public-page>
      <PublicHeader currentPath={en ? "/en/about" : "/hakkimizda"} />

      {/* HERO */}
      <section className={styles.hero}>
        <div className={styles.heroImage} aria-hidden="true" />

        <div className={styles.heroContent}>
          <p className={styles.eyebrow}>{en ? "MEET EKIPHAN" : "EKİPHAN’I TANIYIN"}</p>

          <h1>
            {en ? "From Our Roots" : "Köklerimizden"}
            <br />
            <em>{en ? "to the Future" : "Geleceğe"}</em>
          </h1>

          <div className={styles.heroLine} />

          <p className={styles.heroText}>
            {en ? "Ekiphan listens to the needs of professional kitchens and connects the right equipment with the right project." : "Ekiphan; profesyonel mutfakların ihtiyaçlarını dinleyen, doğru ekipmanı doğru projeyle buluşturan bir çözüm ortağıdır."}
          </p>

          <p className={styles.heroText}>
            {en ? "From planning and procurement to installation and after-sales support, we bring our experience to the rhythm of your operation." : "Planlamadan tedarike, kurulumdan satış sonrası desteğe kadar her adımda deneyimimizi işletmenizin ritmiyle birleştiriyoruz."}
          </p>
        </div>

        <div className={styles.heroNumber}>01</div>
      </section>

      {/* COMPANY */}
      <section className={styles.identity}>
        <div className={styles.sectionIndex}>02</div>

        <div className={styles.sectionLabel}>
          <span>{en ? "WHO WE ARE" : "BİZ KİMİZ?"}</span>
        </div>

        <div className={styles.identityContent}>
          <h2>
            {en ? "A team that" : "İhtiyacı anlayan,"}
            <br />
            {en ? "understands needs" : "çözümü birlikte"}
            <br />
            <em>{en ? "and shapes solutions." : "tasarlayan ekip."}</em>
          </h2>

          <div className={styles.identityCopy}>
            <p>
              {en ? "We combine a considered portfolio of brands with operational expertise for hotel, restaurant, café and commercial kitchen projects." : "Otel, restoran, kafe ve endüstriyel mutfak projelerinde güçlü marka seçkisini operasyonel bilgiyle bir araya getiriyoruz."}
            </p>

            <p>
              {en ? "We account for each project's usage patterns, capacity and goals." : "Her projenin kendine özgü kullanım alışkanlığını, kapasitesini ve hedefini dikkate alıyoruz."}
            </p>

            <p>
              {en ? "Our aim is not only to supply equipment, but to create dependable, efficient workspaces built to last." : "Amacımız yalnızca ekipman tedarik etmek değil; uzun ömürlü, verimli ve güven veren çalışma alanları oluşturmaktır."}
            </p>
          </div>
        </div>
      </section>

      {/* MISSION / VISION */}
      <section className={styles.missionVision}>
        <div className={styles.sectionIndex}>03</div>

        <div className={styles.missionVisionHeader}>
          <span>{en ? "OUR APPROACH" : "YAKLAŞIMIMIZ"}</span>
          <h2>
            {en ? "Meeting today's needs" : "Bugünün ihtiyacını"}
            <br />
            {en ? "with tomorrow's " : "yarının "}<em>{en ? "lasting value" : "değeriyle"}</em>{en ? "." : " buluşturuyoruz."}
          </h2>
        </div>

        <div className={styles.missionVisionGrid}>
          <article>
            <span className={styles.cardNumber}>01</span>
            <p className={styles.eyebrow}>{en ? "OUR MISSION" : "MİSYONUMUZ"}</p>

            <h3>
              {en ? "Delivering functional," : "İşletmeler için işlevsel,"}
              <br />
              {en ? "dependable solutions." : "güvenilir çözümler üretmek."}
            </h3>

            <p className={styles.cardText}>
              {en ? "We pair professional equipment expertise with needs-based planning and dependable implementation." : "Profesyonel ekipman bilgisini, ihtiyaca uygun planlama ve güvenilir uygulamayla buluştururuz."}
            </p>
          </article>

          <article>
            <span className={styles.cardNumber}>02</span>
            <p className={styles.eyebrow}>{en ? "OUR VISION" : "VİZYONUMUZ"}</p>

            <h3>
              {en ? "Creating lasting value" : "Her projede kalıcı değer"}
              <br />
              {en ? "in every project." : "yaratan çözüm ortağı olmak."}
            </h3>

            <p className={styles.cardText}>
              {en ? "We build sustainable partnerships through selected brands and a strong service culture in the HoReCa sector." : "HoReCa dünyasında seçkin markalar ve güçlü hizmet anlayışıyla sürdürülebilir iş birlikleri kurarız."}
            </p>
          </article>
        </div>
      </section>

      {/* VALUES */}
      <section className={styles.values}>
        <div className={styles.sectionIndex}>04</div>

        <div className={styles.valuesHeader}>
          <p className={styles.eyebrow}>{en ? "OUR VALUES" : "DEĞERLERİMİZ"}</p>

          <h2>
            {en ? "The same principles" : "Her projede aynı"}
            <br />
            {en ? "guide our work." : <>çalışma <em>ilkeleri.</em></>}
          </h2>
        </div>

        <div className={styles.valuesGrid}>
          {displayedValues.map(([number, title, text]) => (
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
          alt={en ? "Ekiphan showroom" : "Ekiphan showroom"}
          loading="lazy"
        />

        <div className={styles.ctaOverlay} />

        <div className={styles.ctaContent}>
          <p className={styles.eyebrow}>{en ? "LET'S PLAN TOGETHER" : "BİRLİKTE PLANLAYALIM"}</p>

          <h2>
            {en ? "Let's discuss your needs," : "İhtiyacınızı konuşalım,"}
            <br />
            {en ? <>and build the right solution <em>together.</em></> : <>doğru çözümü <em>birlikte kuralım.</em></>}
          </h2>

          <Link href={en ? "/en/contact#contact-form" : "/iletisim#contact-form"}>
            {en ? "GET IN TOUCH" : "İLETİŞİME GEÇİN"}
            <span>↗</span>
          </Link>
        </div>
      </section>
    </main>
  );
}
