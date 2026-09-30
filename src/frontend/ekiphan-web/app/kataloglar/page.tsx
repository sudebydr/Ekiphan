import type { Metadata } from "next";
import { PublicHeader } from "../../components/public-header";
import { CatalogGallery } from "./catalog-gallery";
import styles from "./catalogs.module.css";

export const metadata: Metadata = {
  title: "Kataloglar | Ekiphan",
  description: "Ekiphan profesyonel mutfak çözümleri katalogları."
};

const catalogs = [
  { id: "acik-bufe", category: "Ekiphan katalogları", eyebrow: "EKİPHAN · 2026", title: "Ekiphan açık büfe", description: "Açık büfe sunum çözümleri.", image: "/images/catalog-product-worlds-ai.png", pdfUrl: "/catalogs/Ekiphan-açık büfe.pdf" },
  { id: "bar", category: "Ekiphan katalogları", eyebrow: "EKİPHAN · 2026", title: "Ekiphan bar", description: "Bar ve servis alanı seçkileri.", image: "/images/catalog-products-hero-v2.png", pdfUrl: "/catalogs/Ekiphan-bar.pdf" },
  { id: "cihazlar", category: "Ekiphan katalogları", eyebrow: "EKİPHAN · 2026", title: "Ekiphan cihazlar", description: "Profesyonel mutfak ekipmanları.", image: "/images/industrial-kitchen-premium.png", pdfUrl: "/catalogs/Ekiphan-cihazlar.pdf" },
  { id: "restaurant", category: "Ekiphan katalogları", eyebrow: "EKİPHAN · 2026", title: "Restaurant malzemeleri", description: "Restoran kullanımına yönelik ürün seçkileri.", image: "/images/feature-selection.png", pdfUrl: "/catalogs/Ekiphan_Restaurant Malzemeleri.pdf" },
  { id: "klasik", category: "Ekiphan katalogları", eyebrow: "EKİPHAN · 2026", title: "Klasik seçki", description: "Zamansız servis ve sunum ürünleri.", image: "/images/catalog-product-worlds-ai.png", pdfUrl: "/catalogs/Ekiphan_catal_kasik_2026.pdf" },
  { id: "kutahya", category: "Marka katalogları", eyebrow: "MARKA KATALOĞU · 2026", title: "Kütahya Porselen", description: "Porselen ürün seçkileri.", image: "/images/feature-selection.png", pdfUrl: "/catalogs/Kütahya-Porselen-2026.pdf" },
  { id: "bonna", category: "Marka katalogları", eyebrow: "MARKA KATALOĞU · 2026", title: "Bonna", description: "Profesyonel porselen koleksiyonu.", image: "/images/catalog-products-hero-v2.png", pdfUrl: "/catalogs/bonna-katalog-2026.pdf" },
  { id: "esma", category: "Marka katalogları", eyebrow: "MARKA KATALOĞU · 2026", title: "Esmadereboy", description: "Masa üstü ürün seçkileri.", image: "/images/catalog-product-worlds-ai.png", pdfUrl: "/catalogs/esmadereboy-2026.pdf" },
  { id: "fabrika", category: "Marka katalogları", eyebrow: "MARKA KATALOĞU · 2026", title: "Fabrika", description: "Mutfak çözümleri kataloğu.", image: "/images/industrial-kitchen-premium.png", pdfUrl: "/catalogs/fabrika-2026.pdf" },
  { id: "nude", category: "Marka katalogları", eyebrow: "MARKA KATALOĞU · 2026", title: "Nude", description: "Cam ve sunum ürünleri.", image: "/images/feature-selection.png", pdfUrl: "/catalogs/nude-2026.pdf" },
  { id: "pasabahce", category: "Marka katalogları", eyebrow: "MARKA KATALOĞU · 2026", title: "Paşabahçe", description: "Servis ve bardak koleksiyonu.", image: "/images/catalog-products-hero-v2.png", pdfUrl: "/catalogs/paşabahçe-2026.pdf" },
  { id: "selene", category: "Marka katalogları", eyebrow: "MARKA KATALOĞU · 2026", title: "Selene", description: "Seçili ürün katalogları.", image: "/images/catalog-product-worlds-ai.png", pdfUrl: "/catalogs/selene_11.06.26.pdf" }
];

export default function CatalogsPage() {
  return (
    <main className={styles.page} data-public-page>
      <PublicHeader currentPath="/kataloglar" />

      <section className={styles.hero} aria-labelledby="catalogs-title">
        <div className={styles.heroIndex} aria-hidden="true">01</div>
        <div className={styles.heroCopy}>
          <div className={styles.heroEyebrow}>
            <span>KATALOG ARŞİVİ</span>
            <i aria-hidden="true" />
            <span>EKİPHAN</span>
          </div>
          <h1 id="catalogs-title">
            İlham veren
            <em>seçkiler.</em>
          </h1>
          <p>
            Profesyonel mutfaklar, servis alanları ve marka koleksiyonları için
            hazırladığımız katalogları tek bir yerde keşfedin.
          </p>
        </div>
        <div className={styles.heroSide}>
          <strong>12</strong>
          <span>KATALOG</span>
          <div className={styles.heroLine} aria-hidden="true" />
          <p>PDF önizlemesi sayfadan ayrılmadan açılır.</p>
        </div>
      </section>

      <CatalogGallery catalogs={catalogs} />
    </main>
  );
}
