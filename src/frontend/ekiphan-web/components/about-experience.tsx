import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "./public-header";
import styles from "./about-experience.module.css";

type Locale = "tr" | "en";

const content = {
  tr: {
    path: "/hakkimizda", title: "Hakkımızda", home: "Ana Sayfa", crumb: "Hakkımızda",
    eyebrow: "EKİPHAN'I TANIYIN", storyTitle: "Profesyonel mutfakların güvenilir çözüm ortağı.",
    story: "Ekiphan, otel, restoran ve profesyonel işletmeler için ekipman seçiminden proje uygulamasına kadar uçtan uca çözümler geliştirir. Seçkin markalar, operasyonel bilgi ve titiz proje yönetimini aynı masada buluştururuz.",
    storySecond: "Her alanın kendine özgü ritmini dinler; kalite standartlarından ödün vermeden, uzun ömürlü ve verimli sistemler tasarlarız. Amacımız yalnızca ekipman tedarik etmek değil, misafir deneyimine kalıcı değer katmaktır.",
    metrics: [["25+", "Deneyim Yılı"], ["20.000+", "Ürün Çeşidi"], ["1.500+", "Tamamlanan Proje"], ["%100", "Müşteri Memnuniyeti"]],
    mission: "Misyonumuz", missionText: "Profesyonel işletmelerin ihtiyaçlarını doğru anlayarak; işlevsel, kaliteli ve güvenilir ekipman çözümleri sunmak.",
    vision: "Vizyonumuz", visionText: "HoReCa sektöründe yenilikçi yaklaşımı, güçlü iş ortaklıkları ve kusursuz hizmet standardıyla referans gösterilen marka olmak.",
    values: "Değerlerimiz", team: "Ekibimiz", teamLead: "Her projenin arkasında; detayları önemseyen, çözüm üreten bir ekip var.", certificates: "Sertifikalar ve Belgeler", certificateLead: "Kalite yaklaşımımızı destekleyen belgeler ve uygunluk standartları.",
    valueItems: [["◇", "Kalite & Güven", "Her işte izlenebilir kalite ve güvenilir iş ortaklığı."], ["✦", "Yenilikçilik", "İhtiyaçları öngören, çağdaş çözümler."], ["↗", "Müşteri Odaklılık", "Projeye ve işletmeye özel, erişilebilir destek."], ["⌁", "Sürdürülebilirlik", "Uzun ömürlü seçimlerle kalıcı değer."]],
    roles: [["AS", "Ayşe Sönmez", "Genel Müdür"], ["MK", "Mert Kaya", "Proje Direktörü"], ["ED", "Elif Demir", "Satış ve Operasyon Direktörü"]],
    certificateItems: ["ISO 9001", "CE", "Kalite Güvence", "Uygunluk Belgeleri"], alt: "Ekiphan showroomunda profesyonel mutfak ekipmanları"
  },
  en: {
    path: "/en/about", title: "About Us", home: "Home", crumb: "About Us",
    eyebrow: "MEET EKİPHAN", storyTitle: "The trusted partner of professional kitchens.",
    story: "Ekiphan develops end-to-end solutions for hotels, restaurants and professional businesses, from equipment selection to project delivery. We bring together selected brands, operational knowledge and disciplined project management.",
    storySecond: "We listen to the unique rhythm of every space and design efficient, long-lasting systems without compromising quality standards. Our purpose is not only to supply equipment, but to create lasting value for the guest experience.",
    metrics: [["25+", "Years of Experience"], ["20,000+", "Product Varieties"], ["1,500+", "Completed Projects"], ["100%", "Customer Satisfaction"]],
    mission: "Our Mission", missionText: "To understand the needs of professional businesses and deliver functional, high-quality and dependable equipment solutions.",
    vision: "Our Vision", visionText: "To be a benchmark HoReCa brand through an innovative approach, strong partnerships and an exceptional service standard.",
    values: "Our Values", team: "Our Team", teamLead: "Behind every project is a team that cares about detail and creates solutions.", certificates: "Certificates & Documents", certificateLead: "Documents and compliance standards supporting our quality approach.",
    valueItems: [["◇", "Quality & Trust", "Traceable quality and dependable partnerships in every project."], ["✦", "Innovation", "Contemporary solutions that anticipate needs."], ["↗", "Customer Focus", "Accessible support tailored to every operation."], ["⌁", "Sustainability", "Long-term value through durable choices."]],
    roles: [["AS", "Ayşe Sönmez", "General Manager"], ["MK", "Mert Kaya", "Project Director"], ["ED", "Elif Demir", "Sales & Operations Director"]],
    certificateItems: ["ISO 9001", "CE", "Quality Assurance", "Compliance Documents"], alt: "Professional kitchen equipment in the Ekiphan showroom"
  }
} as const;

export function aboutMetadata(locale: Locale): Metadata {
  const c = content[locale];
  return { title: c.title, description: c.story, alternates: { canonical: c.path, languages: { tr: "/hakkimizda", en: "/en/about" } } };
}

export function AboutExperience({ locale }: { locale: Locale }) {
  const c = content[locale];
  return <main className={styles.page} data-public-page>
    <PublicHeader currentPath={c.path} />
    <section className={styles.hero} aria-labelledby="about-title" data-cms-section="about-hero">
      <nav className={styles.breadcrumb} aria-label="Breadcrumb"><Link href={locale === "tr" ? "/" : "/en"}>{c.home}</Link><span>/</span><span>{c.crumb}</span></nav>
      <p className={styles.kicker}>{c.eyebrow}</p><h1 id="about-title">{c.title}</h1><i className={styles.accent} aria-hidden="true" />
    </section>
    <section className={styles.story} aria-labelledby="story-title" data-cms-section="company-story">
      <div className={styles.storyImage}><img src="/images/gallery/showroom-2026-08-11/TEK_9546%20copy.jpg" alt={c.alt} width={1600} height={1100} /></div>
      <div className={styles.storyCopy}><p className={styles.kicker}>{c.eyebrow}</p><h2 id="story-title">{c.storyTitle}</h2><div data-cms-field="about-story"><p>{c.story}</p><p>{c.storySecond}</p></div></div>
    </section>
    <section className={styles.metrics} aria-label="Ekiphan metrics" data-cms-section="about-metrics">{c.metrics.map(([number, label]) => <article key={label}><strong>{number}</strong><span>{label}</span></article>)}</section>
    <section className={styles.missionVision} aria-label={`${c.mission} and ${c.vision}`} data-cms-section="mission-vision">
      <article><span className={styles.featureIcon}>◒</span><p className={styles.kicker}>{c.mission}</p><h2>{c.missionText}</h2></article>
      <article><span className={styles.featureIcon}>◌</span><p className={styles.kicker}>{c.vision}</p><h2>{c.visionText}</h2></article>
    </section>
    <section className={styles.section} aria-labelledby="values-title" data-cms-section="core-values"><header><p className={styles.kicker}>EKİPHAN</p><h2 id="values-title">{c.values}</h2></header><div className={styles.valueGrid}>{c.valueItems.map(([icon, title, description]) => <article key={title}><span>{icon}</span><h3>{title}</h3><p>{description}</p></article>)}</div></section>
    <section className={`${styles.section} ${styles.teamSection}`} aria-labelledby="team-title" data-cms-section="leadership-team"><header><p className={styles.kicker}>EKİPHAN</p><h2 id="team-title">{c.team}</h2><p>{c.teamLead}</p></header><div className={styles.teamGrid}>{c.roles.map(([initials, name, role], index) => <article key={name}><div className={`${styles.portrait} ${styles[`portrait${index}`]}`} aria-label={`${name} photo placeholder`}><span>{initials}</span></div><div><h3>{name}</h3><p>{role}</p><a href="#linkedin" aria-label={`${name} LinkedIn`}>in</a></div></article>)}</div></section>
    <section className={styles.certificates} aria-labelledby="certificates-title" data-cms-section="certificates"><div><p className={styles.kicker}>EKİPHAN</p><h2 id="certificates-title">{c.certificates}</h2><p>{c.certificateLead}</p></div><div className={styles.certificateGrid}>{c.certificateItems.map((item) => <a href="#certificate-preview" key={item} aria-label={`${item} preview`}><span>✓</span><strong>{item}</strong><small>PDF</small></a>)}</div></section>
  </main>;
}
