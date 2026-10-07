import type { Metadata } from "next";
import Link from "next/link";

import { PublicHeader } from "../../components/public-header";
import { MetricCounters } from "../../components/metric-counters";
import { ScrollReveal } from "../../components/scroll-reveal";

import {
  getCatalogNavigation,
  getContentPage,
} from "../../lib/catalog-api";

import type { CatalogNavigation } from "../../lib/catalog-types";

import type { PublicContentPage } from "../../lib/content-types";

import { defaultSocialImage } from "../../lib/social-metadata";

import styles from "../home.module.css";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  alternates: {
    canonical: "/en",
  },

  openGraph: {
    type: "website",
    locale: "en_US",
    title: "Ekiphan | Professional Hotel and Kitchen Equipment",
    description:
      "Professional equipment for hotels, restaurants and commercial kitchens.",
    images: defaultSocialImage ? [defaultSocialImage] : undefined,
    url: "/en",
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
  const [
    heroResult,
    navigationResult,
  ] = await Promise.allSettled([
    getContentPage("ana-sayfa", "en"),
    getCatalogNavigation("en"),
  ]);

  if (heroResult.status === "fulfilled") {
    managedHero = heroResult.value;
  }

  if (navigationResult.status === "fulfilled") {
    navigation = navigationResult.value;
  }

  const heroTitle =
    managedHero?.title ??
    "Professional Solutions, Lasting Value";

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

        <PublicHeader currentPath="/en" />

        <div className={styles.heroGrid}>
          <div className={styles.heroCopy}>
            <p className={styles.heroKicker}>
              FOR PROFESSIONAL KITCHENS
            </p>

            <h1>
              <span>{heroTitleLead}</span>

              {heroTitleAccent && (
                <em>{heroTitleAccent}</em>
              )}
            </h1>

            <div className={styles.heroLine} />

            <p className={styles.heroLead}>
              {managedHero?.summary ??
                "With experience, selected brands and a project-led approach, we create lasting value for hotels, restaurants and commercial kitchens."}
            </p>

            <div className={styles.heroActions}>
              <Link
                href={
                  "/en/products"
                }
                className={styles.primaryButton}
              >
                <span>
                  Explore Products
                </span>

                <i aria-hidden="true">→</i>
              </Link>

              <Link
                href={
                  "/en/contact"
                }
                className={styles.textButton}
              >
                <span>
                  Request a Quote
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
            PLANNING
            <span>|</span>
            IMPLEMENTATION
            <span>|</span>
            SUPPORT
          </div>
        </div>
      </section>

      {/* =====================================================
          METRICS
      ===================================================== */}

      <section
        className={styles.metrics}
        aria-label="Ekiphan in numbers"
        data-reveal
      >
        <MetricCounters locale="en" />
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
                  CATEGORIES
                </p>

                <h2 id="categories-title">
                  Solutions for
                  <br />
                  professional
                  <br />
                  <em>kitchens.</em>
                </h2>
              </div>

              <div className={styles.categoriesDescription}>
                <p>
                  We bring together high-performance,
                  durable and efficient equipment for
                  commercial kitchens.
                </p>

                <Link href="/en/products">
                  EXPLORE ALL PRODUCTS
                  <span aria-hidden="true">→</span>
                </Link>
              </div>

              <Link
                href="/en/catalogs"
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
                    EXPLORE CATALOG
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
                      href="/en/catalogs"
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
                          EXPLORE CATALOG
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
            alt="Ekiphan professional kitchen solutions"
            loading="lazy"
          />
        </div>

        <div className={styles.aboutContent}>
          <p className={styles.eyebrow}>
            ABOUT EKİPHAN
          </p>

          <h2>
            Better
            <br />
            kitchens
            <br />
            <em>for everyone.</em>
          </h2>

          <p className={styles.aboutAccent}>
            Planned. Reliable. Sustainable.
          </p>

          <p className={styles.aboutText}>
            With years of experience, strong partners
            and an expert team, we support professional
            kitchen projects from design and installation
            through after-sales service, acting as a
            reliable partner at every stage.
          </p>

          <Link
            href="/en/about"
            className={styles.editorialLink}
          >
            GET TO KNOW EKİPHAN
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
            PROJECT SUPPORT SERVICES
          </p>

          <h2>
            From concept
            <br />
            <em>to completion.</em>
          </h2>

          <p>
            Our expert team supports your kitchen
            project through site surveys, planning,
            equipment selection and installation.
          </p>

          <Link
            href="/en/contact#contact-form"
            className={styles.lightButton}
          >
            LET'S DISCUSS YOUR PROJECT
            <span aria-hidden="true">→</span>
          </Link>
        </div>

        <div className={styles.projectSideText}>
          THE RIGHT PLAN.
          <br />
          LONG-LASTING
          <br />
          SOLUTIONS.
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
              WHY EKİPHAN?
            </p>

            <h2 id="why-ekiphan">
              Your trusted partner in
              <br />
              <em>professional projects.</em>
            </h2>
          </div>

          <p>
            We bring together quality, engineering
            and lasting performance at every stage,
            from design to after-sales support.
          </p>
        </div>

        <div className={styles.featureList}>
          <article className={styles.featureItem}>
            <span>01</span>

            <div>
              <h3>Exceptional Quality</h3>

              <p>
                We bring select brands to your projects
                with refined workmanship and high
                performance standards.
              </p>
            </div>

            <Link href="/en/products">
              Explore details
              <i aria-hidden="true">→</i>
            </Link>
          </article>

          <article className={styles.featureItem}>
            <span>02</span>

            <div>
              <h3>Tailored Solutions</h3>

              <p>
                We develop solutions tailored to your
                space with bespoke planning, equipment
                selection and implementation support.
              </p>
            </div>

            <Link href="/en/references">
              View projects
              <i aria-hidden="true">→</i>
            </Link>
          </article>

          <article className={styles.featureItem}>
            <span>03</span>

            <div>
              <h3>Ongoing Support</h3>

              <p>
                Our technical service and spare parts
                support help keep your operations
                running smoothly.
              </p>
            </div>

            <Link href="/en/contact">
              Get support
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
            MEET EKİPHAN
          </p>
          <h2>
            The trusted partner for
            <br />
            <em>professional kitchens.</em>
          </h2>

          <p>
            With experience, selected brands and a
            project-led approach, we create lasting
            value for hotels, restaurants and
            commercial kitchens.
          </p>

          <Link
            href="/en/gallery?category=Showroom"
            className={styles.showroomButton}
          >
            VISIT THE SHOWROOM
            <span aria-hidden="true">→</span>
          </Link>
        </div>
      </section>
    </main>
  );
}
