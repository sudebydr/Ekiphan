import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { PublicHeader } from "../../components/public-header";
import { PublicNavigation } from "../../components/public-navigation";
import { CatalogApiError, getContentPage } from "../../lib/catalog-api";
import { managedMetadata } from "../../lib/managed-metadata";
import type { PublicContentPage } from "../../lib/content-types";
import styles from "./corporate.module.css";

export const dynamic = "force-dynamic";

const routes = {
  hakkimizda: "hakkimizda",
  hizmetler: "hizmetler",
  referanslar: "referanslar",
  galeri: "galeri",
  "basin-odasi": "basin-odasi",
  iletisim: "iletisim"
} as const;

type Props = { params: Promise<{ corporate: string }> };

const contentSlug = (route: string): string | null =>
  routes[route as keyof typeof routes] ?? null;

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { corporate } = await params;
  const slug = contentSlug(corporate);
  if (!slug) return { robots: { index: false, follow: false } };
  try {
    const page = await getContentPage(slug);
    const path = `/${corporate}`;
    return {
      title: page.metaTitle ?? page.title,
      description: page.metaDescription ?? page.summary,
      ...managedMetadata(page, path)
    };
  } catch {
    return { title: "Kurumsal İçerik", robots: { index: false, follow: false } };
  }
}

const aboutFallback: PublicContentPage = { id: "demo-about", code: "hakkimizda", languageCode: "tr", title: "Profesyonel mutfağın ardındaki deneyim.", slug: "hakkimizda", summary: "Profesyonel mutfakların ihtiyaçlarını tasarımdan uygulamaya kadar bütüncül çözümlerle karşılıyoruz.", body: "Ekiphan; profesyonel mutfak ekipmanları, proje uygulamaları ve servis çözümleri alanında işletmelerin ihtiyaçlarına bütüncül yaklaşan bir çözüm ortağıdır.`n`nPlanlamadan ürün seçimine, uygulamadan satış sonrası desteğe kadar süreçlerin her aşamasında işlevsellik, dayanıklılık ve sürdürülebilir kullanım odağında çalışır.", metaTitle: null, metaDescription: null, canonicalUrl: null, noIndex: false, noFollow: false, updatedAt: "", openGraphTitle: null, openGraphDescription: null, openGraphImageMediaId: null, openGraphImageUrl: null, alternates: null };

export default async function CorporatePage({ params }: Props) {
  const { corporate } = await params;
  const slug = contentSlug(corporate);
  if (!slug) notFound();
  let page: PublicContentPage;
  try {
    page = await getContentPage(slug);
  } catch (error) {
    if (error instanceof CatalogApiError && error.status === 404) notFound();
    return (
      <main className={`${styles.page} ${corporate === "hakkimizda" ? styles.aboutPage : ""}`} data-public-page>
        <PublicHeader />
        <section className={styles.hero}>
          <p className={styles.eyebrow}>EKİPHAN · KURUMSAL</p>
          <h1>İçerik şu anda görüntülenemiyor.</h1>
          <p>Bağlantı yeniden kurulduğunda kurumsal içerik burada gösterilecek.</p>
        </section>
        <div className={styles.error} role="alert">
          <strong>İçerik yüklenemedi.</strong>
          <p>{error instanceof CatalogApiError ? error.message : "Lütfen daha sonra tekrar deneyin."}</p>
        </div>
      </main>
    );
  }

  const paragraphs = page.body.split(/\r?\n{2,}/)
    .map((item) => item.trim()).filter(Boolean);

  return (
    <main className={`${styles.page} ${corporate === "hakkimizda" ? styles.aboutPage : ""}`} data-public-page>
      <PublicHeader currentPath={`/${corporate}`} />
      {corporate === "hakkimizda" && (
        <section className={styles.aboutLayout} aria-labelledby="about-title">
          <div className={styles.aboutImage}>
            <img src="/images/gallery/showroom-2026-08-11/TEK_9403%20copy.jpg" alt="Ekiphan showroomundan profesyonel mutfak detayı" width={1200} height={900} />
          </div>
          <div className={styles.aboutCopy}>
            <p className={styles.aboutKicker}>EKİPHAN'I TANIYIN / HAKKIMIZDA</p>
            <h1 id="about-title">HAKKIMIZDA</h1>
            <p className={styles.aboutLead}>Profesyonel mutfakların ihtiyaçlarını planlamadan uygulamaya kadar bütüncül çözümlerle karşılıyoruz.</p>
            <p>Ekiphan; otel, restoran ve profesyonel işletmeler için ekipman seçimi, proje planlama ve uygulama süreçlerinde güvenilir bir çözüm ortağıdır.</p>
            <p>İşlevsellik, dayanıklılık ve uzun ömürlü kullanım odağında; her projenin ihtiyaçlarına uygun, sade ve sistemli çözümler geliştiriyoruz.</p>
            <p>Ürün seçiminden satış sonrası desteğe kadar her aşamada, projelerin sürdürülebilir biçimde işlemesine katkı sağlamayı hedefliyoruz.</p>
          </div>
        </section>
      )}
      <section className={styles.hero}>
        <p className={styles.eyebrow}>EKİPHAN · KURUMSAL</p>
        <h1>{page.title}</h1>
        {page.summary && <p>{page.summary}</p>}
      </section>
      <article className={styles.content}>
        {paragraphs.map((paragraph, index) => (
          <p key={`${index}-${paragraph.slice(0, 24)}`}>{paragraph}</p>
        ))}
      </article>
    </main>
  );
}
