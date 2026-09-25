import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../components/public-header";
import { MetricCounters } from "../components/metric-counters";
import { PublicNavigation } from "../components/public-navigation";
import { ScrollReveal } from "../components/scroll-reveal";
import {
  getBrands,
  getCatalogNavigation,
  getContentPage,
  getHomepageHeroes
} from "../lib/catalog-api";
import type {
  CatalogBrandListItem,
  CatalogNavigation
} from "../lib/catalog-types";
import type { PublicContentPage } from "../lib/content-types";
import { defaultSocialImage } from "../lib/social-metadata";
import styles from "./home.module.css";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  alternates: {
    canonical: "/"
  },
  openGraph: {
    type: "website",
    locale: "tr_TR",
    title: "Ekiphan | Profesyonel Otel ve Mutfak Ekipmanları",
    description:
      "Otel, restoran ve endüstriyel mutfaklar için profesyonel ekipman kataloğu.",
    images: defaultSocialImage ? [defaultSocialImage] : undefined,
    url: "/"
  }
};

export default async function HomePage() {
  let managedHero: PublicContentPage | null = null;
  let navigation: CatalogNavigation | null = null;
  let brands: CatalogBrandListItem[] = [];

  const [heroResult, navigationResult, brandsResult, structuredHeroResult] =
    await Promise.allSettled([
      getContentPage("ana-sayfa"),
      getCatalogNavigation(),
      getBrands(),
      getHomepageHeroes()
    ]);
  if (heroResult.status === "fulfilled") managedHero = heroResult.value;
  if (navigationResult.status === "fulfilled") {
    navigation = navigationResult.value;
  }
  if (brandsResult.status === "fulfilled") brands = brandsResult.value;
  const structuredHero =
    structuredHeroResult.status === "fulfilled"
      ? structuredHeroResult.value[0] ?? null
      : null;
  const heroTitle =
    structuredHero?.title ??
    managedHero?.title ??
    "Profesyonel Çözümler, Kalıcı Değerler";
  const [heroTitleLead, ...heroTitleAccentParts] = heroTitle.split(",");
  const heroTitleAccent = heroTitleAccentParts.join(",").trim();

  return (
    <main className={styles.page}>
      <ScrollReveal />
      <section className={styles.hero}>
        <div className={styles.visual} aria-hidden="true">
          <video
            autoPlay
            loop
            muted
            playsInline
            preload="auto"
          >
            <source src="/videos/ekiphan-hero-custom.mp4" type="video/mp4" />
          </video>
        </div>
        <div className={styles.heroShade} aria-hidden="true" />

        <PublicHeader currentPath="/" />

        <div className={styles.heroCopy}>
          <p className={styles.heroKicker}>PROFESYONEL MUTFAKLARA</p>
          <h1>
            <span>{heroTitleLead}</span>
            {heroTitleAccent && <em>{heroTitleAccent}</em>}
          </h1>
          <p className={styles.lead}>
            {structuredHero?.subtitle ??
              managedHero?.summary ??
              "Ekiphan’ın profesyonel otel ve restoran ekipmanları çözümleriyle tanışın. Endüstriyel mutfaklarda mükemmelliği yeniden tanımlıyoruz."}
          </p>
          <div className={styles.actions}>
            <Link
              className={styles.primary}
              href={structuredHero?.primaryCtaUrl ?? "/katalog"}
            >
              <span>
                {structuredHero?.primaryCtaLabel ?? "Ürünleri İncele"}
              </span>
            </Link>
            <Link
              className={styles.secondary}
              href={structuredHero?.secondaryCtaUrl ?? "/iletisim"}
            >
              <span>{structuredHero?.secondaryCtaLabel ?? "Teklif Al"}</span>
            </Link>
          </div>
        </div>

      </section>
      {navigation && navigation.sections.length > 0 && (
        <section className={styles.productWorlds} aria-labelledby="worlds-title" data-reveal>
          <div className={styles.sectionHeading}>
  <div className={styles.worldLead}>
    <p className={styles.eyebrow}>ÖZENLE SEÇİLENLER</p>
    <p>Profesyonel mutfak ve servis dünyasının öne çıkanlarını sizin için bir araya getiriyoruz.</p>
  </div>
  <div className={styles.worldTitle}>
    <h2 id="worlds-title"><em>Doğru çözümü keşfedin.</em></h2>
    <p>Öne çıkan ürünlerden anahtar teslim mutfak projelerine kadar aradığınız her çözüm burada.</p>
  </div>
</div>
          <div className={styles.worldGrid}>
  <Link className={styles.worldFeature} href="/katalog">
    <span>Seçili Koleksiyon</span>
    <strong>Öne Çıkan Ürünler</strong>
    <small>Ürünleri keşfet <i aria-hidden="true">→</i></small>
  </Link>
  <Link className={`${styles.worldFeature} ${styles.industrialFeature}`} href="/iletisim">
    <span>Anahtar Teslim Çözümler</span>
    <strong>Endüstriyel Mutfak Projeleri</strong>
    <small>Projeleri incele <i aria-hidden="true">→</i></small>
  </Link>
            {navigation.sections.map((section) => (
              <Link
                href="/kataloglar"
                key={section.id}
              >
                <span>{section.code}</span>
                <strong>{section.name}</strong>
                <small>Kataloğu keşfet →</small>
              </Link>
            ))}
          </div>
        </section>
      )}

      <section className={styles.metrics} aria-label="Ekiphan sayılarla" data-reveal>
        <MetricCounters />
      </section>

<section
        className={styles.features}
        id="yaklasim"
        aria-labelledby="why-ekiphan"
        data-reveal
      >
        <div className={styles.featuresHeading} data-reveal>
          <p className={styles.featuresBadge}><i aria-hidden="true" />NEDEN EKİPHAN?<span aria-hidden="true" /></p>
          <h2 id="why-ekiphan">
            Profesyonel projelerde <em>güvenilir</em> çözüm ortağı.
          </h2>
          <p className={styles.featuresLead}>
            Tasarımdan satış sonrası desteğe kadar her aşamada kalite, mühendislik
            ve uzun ömürlü performansı bir araya getiriyoruz.
          </p>

        </div>
        <article className={`${styles.feature} ${styles.featureQuality}`} data-reveal data-reveal-delay="1">
          <div className={styles.featureContent}>
            <span className={styles.featureIcon} aria-hidden="true">◇</span>
            <h3>Üstün Kalite</h3>
            <p>Seçkin markaları, kusursuz işçilik ve yüksek performans standardıyla projelerinize taşıyoruz.</p>
            <Link href="/katalog" className={styles.featureLink}>Detayları Keşfet <i aria-hidden="true">→</i></Link>
          </div>
          <div className={styles.featureVisual} aria-hidden="true">
            <img src="/images/feature-kitchen-triptych-v1.png" alt="" width={1536} height={1024} loading="lazy" />
          </div>
        </article>
        <article className={`${styles.feature} ${styles.featureProjects}`} data-reveal data-reveal-delay="2">
          <div className={styles.featureContent}>
            <span className={styles.featureIcon} aria-hidden="true">⌁</span>
            <h3>Terzi İşi Çözümler</h3>
            <p>İhtiyacınıza özel planlama, ürün seçimi ve uygulama desteğiyle mekânınıza özgü çözümler geliştiriyoruz.</p>
            <Link href="/referanslar" className={styles.featureLink}>Projeleri Gör <i aria-hidden="true">→</i></Link>
          </div>
          <div className={styles.featureVisual} aria-hidden="true">
            <img src="/images/feature-kitchen-triptych-v1.png" alt="" width={1536} height={1024} loading="lazy" />
          </div>
        </article>
        <article className={`${styles.feature} ${styles.featureSupport}`} data-reveal data-reveal-delay="3">
          <div className={styles.featureContent}>
            <span className={styles.featureIcon} aria-hidden="true">✓</span>
            <h3>Sürekli Destek</h3>
            <p>Teknik servis ve yedek parça güvencemizle operasyonunuzun kesintisiz devam etmesini sağlıyoruz.</p>
            <Link href="/iletisim" className={styles.featureLink}>Destek Al <i aria-hidden="true">→</i></Link>
          </div>
          <div className={styles.featureVisual} aria-hidden="true">
            <img src="/images/feature-kitchen-triptych-v1.png" alt="" width={1536} height={1024} loading="lazy" />
          </div>
        </article>
      </section>

      <section id="showroom" className={styles.showroomCta} data-reveal>
        <img
          src="/images/showroom-experience.png"
          alt=""
          width={1792}
          height={1024}
          loading="lazy"
        />
        <div data-reveal data-reveal-delay="1">
          <p>EKİPHAN’I TANIYIN</p>
          <h2>Profesyonel mutfakların güvenilir çözüm ortağı.</h2>
          <p>
            Deneyim, seçkin markalar ve projeye özel yaklaşımımızla otel,
            restoran ve endüstriyel mutfaklara kalıcı değer katıyoruz.
          </p>
          <Link href="/galeri?category=Showroom">Showroom Turu</Link>
        </div>
      </section>
    </main>
  );
}


