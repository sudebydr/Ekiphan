import type { Metadata } from "next";
import Link from "next/link";

import { PublicHeader } from "../components/public-header";
import { MetricCounters } from "../components/metric-counters";
import { ScrollReveal } from "../components/scroll-reveal";

import {
  getBrands,
  getCatalogNavigation,
  getContentPage,
  getHomepageHeroes,
} from "../lib/catalog-api";

import type {
  CatalogBrandListItem,
  CatalogNavigation,
} from "../lib/catalog-types";

import type { PublicContentPage } from "../lib/content-types";

import { defaultSocialImage } from "../lib/social-metadata";

import styles from "./home.module.css";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  alternates: {
    canonical: "/",
  },

  openGraph: {
    type: "website",
    locale: "tr_TR",
    title: "Ekiphan | Profesyonel Otel ve Mutfak Ekipmanları",
    description:
      "Otel, restoran ve endüstriyel mutfaklar için profesyonel ekipman kataloğu.",
    images: defaultSocialImage ? [defaultSocialImage] : undefined,
    url: "/",
  },
};

const categoryImages = [
  "/images/feature-quality-natural-v1.png",
  "/images/industrial-kitchen-premium.png",
  "/images/feature-selection.png",
  "/images/feature-solutions-natural-v1.png",
  "/images/feature-support-natural-v1.png",
  "/images/ekiphan-kitchen-hero.png",
];

export default async function HomePage() {
  let managedHero: PublicContentPage | null = null;
  let navigation: CatalogNavigation | null = null;
  let brands: CatalogBrandListItem[] = [];

  const [
    heroResult,
    navigationResult,
    brandsResult,
    structuredHeroResult,
  ] = await Promise.allSettled([
    getContentPage("ana-sayfa"),
    getCatalogNavigation(),
    getBrands(),
    getHomepageHeroes(),
  ]);

  if (heroResult.status === "fulfilled") {
    managedHero = heroResult.value;
  }

  if (navigationResult.status === "fulfilled") {
    navigation = navigationResult.value;
  }

  if (brandsResult.status === "fulfilled") {
    brands = brandsResult.value;
  }

  const structuredHero =
    structuredHeroResult.status === "fulfilled"
      ? structuredHeroResult.value[0] ?? null
      : null;

  const heroTitle =
    structuredHero?.title ??
    managedHero?.title ??
    "Profesyonel Çözümler, Kalıcı Değerler";

  const [heroTitleLead, ...heroTitleAccentParts] =
    heroTitle.split(",");

  const heroTitleAccent =
    heroTitleAccentParts.join(",").trim();
  const [featuredCategory, ...remainingCategories] =
    navigation?.sections ?? [];

  return (
    <main className={styles.page}>
      <ScrollReveal />

      {/* =====================================================
          HERO
      ===================================================== */}

      <section className={styles.hero}>
        <div className={styles.heroVisual}>
          <video
            autoPlay
            loop
            muted
            playsInline
            preload="metadata"
            poster="/images/ekiphan-kitchen-hero.png"
          >
            <source
              src="/videos/ekiphan-showroom-hero.mp4"
              type="video/mp4"
            />
          </video>
        </div>

        <div
          className={styles.heroImageShade}
          aria-hidden="true"
        />

        <PublicHeader currentPath="/" />

        <div className={styles.heroGrid}>
          <div className={styles.heroCopy}>
            <p className={styles.heroKicker}>
              PROFESYONEL MUTFAKLAR İÇİN
            </p>

            <h1>
              <span>{heroTitleLead}</span>

              {heroTitleAccent && (
                <em>{heroTitleAccent}</em>
              )}
            </h1>

            <div className={styles.heroLine} />

            <p className={styles.heroLead}>
              {structuredHero?.subtitle ??
                managedHero?.summary ??
                "Deneyim, seçkin markalar ve projeye özel yaklaşımımızla otel, restoran ve endüstriyel mutfaklara kalıcı değer katıyoruz."}
            </p>

            <div className={styles.heroActions}>
              <Link
                href={
                  structuredHero?.primaryCtaUrl ??
                  "/katalog"
                }
                className={styles.primaryButton}
              >
                <span>
                  {structuredHero?.primaryCtaLabel ??
                    "Ürünleri İncele"}
                </span>

                <i aria-hidden="true">→</i>
              </Link>

              <Link
                href={
                  structuredHero?.secondaryCtaUrl ??
                  "/iletisim"
                }
                className={styles.textButton}
              >
                <span>
                  {structuredHero?.secondaryCtaLabel ??
                    "Teklif Al"}
                </span>

                <i aria-hidden="true">↗</i>
              </Link>
            </div>
          </div>

          <div className={styles.heroMeta}>
            <span>01</span>
            <span>02</span>
            <span>03</span>
            <i aria-hidden="true" />
            <span>04</span>
          </div>

          <div className={styles.heroVisualLabel}>
            PLANLAMA
            <span>|</span>
            UYGULAMA
            <span>|</span>
            DESTEK
          </div>
        </div>
      </section>

      {/* =====================================================
          METRICS
      ===================================================== */}

      <section
        className={styles.metrics}
        aria-label="Ekiphan sayılarla"
        data-reveal
      >
        <MetricCounters />
      </section>

      {/* =====================================================
          PRODUCT CATEGORIES
      ===================================================== */}

      {featuredCategory && (
          <section
            className={styles.categories}
            aria-labelledby="categories-title"
            data-reveal
          >
            <div className={styles.categoriesIntro}>
              <div>
                <p className={styles.eyebrow}>
                  KATEGORİLER
                </p>

                <h2 id="categories-title">
                  Profesyonel
                  <br />
                  mutfaklara özel
                  <br />
                  <em>çözümler.</em>
                </h2>
              </div>

              <div className={styles.categoriesDescription}>
                <p>
                  Endüstriyel mutfak için yüksek
                  performanslı, dayanıklı ve verimli
                  ekipmanları bir araya getiriyoruz.
                </p>

                <Link href="/katalog">
                  TÜM ÜRÜNLERİ İNCELE
                  <span aria-hidden="true">→</span>
                </Link>
              </div>

              <Link
                href="/kataloglar"
                className={styles.categoryCard}
              >
                <div className={styles.categoryImage}>
                  <img
                    src={categoryImages[0]}
                    alt=""
                    loading="lazy"
                  />
                </div>

                <div className={styles.categoryContent}>
                  <span>{featuredCategory.code}</span>

                  <h3>{featuredCategory.name}</h3>

                  <small>
                    KATALOĞU KEŞFET
                    <i aria-hidden="true">→</i>
                  </small>
                </div>
              </Link>
            </div>

            {remainingCategories.length > 0 && (
              <div className={styles.categoryGrid}>
                {remainingCategories.map(
                (section, index) => {
                  const image =
                    categoryImages[
                      (index + 1) % categoryImages.length
                    ];

                  return (
                    <Link
                      href="/kataloglar"
                      key={section.id}
                      className={styles.categoryCard}
                    >
                      <div
                        className={
                          styles.categoryImage
                        }
                      >
                        <img
                          src={image}
                          alt=""
                          loading="lazy"
                        />
                      </div>

                      <div
                        className={
                          styles.categoryContent
                        }
                      >
                        <span>
                          {section.code}
                        </span>

                        <h3>{section.name}</h3>

                        <small>
                          KATALOĞU KEŞFET
                          <i aria-hidden="true">
                            →
                          </i>
                        </small>
                      </div>
                    </Link>
                  );
                },
                )}
              </div>
            )}
          </section>
        )}

      {/* =====================================================
          ABOUT
      ===================================================== */}

      <section
        className={styles.about}
        data-reveal
      >
        <div className={styles.aboutImage}>
          <img
            src="/images/why-ekiphan-background.png"
            alt="Ekiphan profesyonel mutfak çözümleri"
            loading="lazy"
          />
        </div>

        <div className={styles.aboutContent}>
          <p className={styles.eyebrow}>
            EKİPHAN HAKKINDA
          </p>

          <h2>
            Daha iyi
            <br />
            mutfaklar
            <br />
            <em>için.</em>
          </h2>

          <p className={styles.aboutAccent}>
            Planlı. Güvenilir. Sürdürülebilir.
          </p>

          <p className={styles.aboutText}>
            Yılların deneyimi, güçlü iş ortaklarımız
            ve uzman ekibimizle profesyonel mutfak
            projelerinde yanınızdayız. Tasarımdan
            kuruluma, satış sonrası desteğe kadar
            tüm süreçlerde güvenilir bir çözüm ortağı
            olarak çalışıyoruz.
          </p>

          <Link
            href="/hakkimizda"
            className={styles.editorialLink}
          >
            EKİPHAN'I TANIYIN
            <span aria-hidden="true">→</span>
          </Link>
        </div>
      </section>

      {/* =====================================================
          PROJECT SUPPORT
      ===================================================== */}

      <section
        className={styles.projectBanner}
        data-reveal
      >
        <img
          src="/images/feature-consultation.png"
          alt=""
          loading="lazy"
        />

        <div className={styles.projectOverlay} />

        <div className={styles.projectContent}>
          <p className={styles.projectEyebrow}>
            PROJE DESTEK HİZMETLERİ
          </p>

          <h2>
            Fikrinizden
            <br />
            <em>uygulamaya.</em>
          </h2>

          <p>
            Mutfak projelerinizde keşif, planlama,
            ürün seçimi ve kurulum süreçlerinde
            uzman ekibimizle yanınızdayız.
          </p>

          <Link
            href="/iletisim#contact-form"
            className={styles.lightButton}
          >
            PROJENİZİ KONUŞALIM
            <span aria-hidden="true">→</span>
          </Link>
        </div>

        <div className={styles.projectSideText}>
          DOĞRU PLAN.
          <br />
          UZUN ÖMÜRLÜ
          <br />
          ÇÖZÜMLER.
        </div>
      </section>

      {/* =====================================================
          WHY EKİPHAN
      ===================================================== */}

      <section
        className={styles.features}
        id="yaklasim"
        aria-labelledby="why-ekiphan"
        data-reveal
      >
        <div className={styles.featuresIntro}>
          <div>
            <p className={styles.eyebrow}>
              NEDEN EKİPHAN?
            </p>

            <h2 id="why-ekiphan">
              Profesyonel projelerde
              <br />
              <em>güvenilir</em> çözüm ortağı.
            </h2>
          </div>

          <p>
            Tasarımdan satış sonrası desteğe kadar
            her aşamada kalite, mühendislik ve
            uzun ömürlü performansı bir araya
            getiriyoruz.
          </p>
        </div>

        <div className={styles.featureList}>
          <article className={styles.featureItem}>
            <span>01</span>

            <div>
              <h3>Üstün Kalite</h3>

              <p>
                Seçkin markaları, kusursuz işçilik
                ve yüksek performans standardıyla
                projelerinize taşıyoruz.
              </p>
            </div>

            <Link href="/katalog">
              Detayları keşfet
              <i aria-hidden="true">→</i>
            </Link>
          </article>

          <article className={styles.featureItem}>
            <span>02</span>

            <div>
              <h3>Terzi İşi Çözümler</h3>

              <p>
                İhtiyacınıza özel planlama, ürün
                seçimi ve uygulama desteğiyle
                mekânınıza özgü çözümler
                geliştiriyoruz.
              </p>
            </div>

            <Link href="/referanslar">
              Projeleri gör
              <i aria-hidden="true">→</i>
            </Link>
          </article>

          <article className={styles.featureItem}>
            <span>03</span>

            <div>
              <h3>Sürekli Destek</h3>

              <p>
                Teknik servis ve yedek parça
                güvencemizle operasyonunuzun
                kesintisiz devam etmesini
                sağlıyoruz.
              </p>
            </div>

            <Link href="/iletisim">
              Destek al
              <i aria-hidden="true">→</i>
            </Link>
          </article>
        </div>
      </section>

      {/* =====================================================
          SHOWROOM / FINAL CTA
      ===================================================== */}

      <section
        id="showroom"
        className={styles.showroom}
        data-reveal
      >
        <img
          src="/images/showroom-experience.png"
          alt=""
          loading="lazy"
        />

        <div className={styles.showroomOverlay} />

        <div className={styles.showroomContent}>
          <p className={styles.showroomEyebrow}>
            EKİPHAN'I TANIYIN
          </p>
          <h2>
            Profesyonel mutfakların
            <br />
            <em>güvenilir çözüm ortağı.</em>
          </h2>

          <p>
            Deneyim, seçkin markalar ve projeye özel
            yaklaşımımızla otel, restoran ve
            endüstriyel mutfaklara kalıcı değer
            katıyoruz.
          </p>

          <Link
            href="/galeri?category=Showroom"
            className={styles.showroomButton}
          >
            SHOWROOM TURU
            <span aria-hidden="true">→</span>
          </Link>
        </div>
      </section>
    </main>
  );
}
