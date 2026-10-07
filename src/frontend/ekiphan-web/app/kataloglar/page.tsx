import type { Metadata } from "next";
import { PublicHeader } from "../../components/public-header";
import { CatalogGallery } from "./catalog-gallery";
import { getCatalogPdfDocuments } from "../../lib/catalog-api";
import styles from "./catalogs.module.css";

export const metadata: Metadata = {
  title: "Kataloglar | Ekiphan",
  description: "Ekiphan profesyonel mutfak çözümleri katalogları."
};

export const dynamic = "force-dynamic";

export default async function CatalogsPage() {
  const uploadedCatalogs = await getCatalogPdfDocuments();
  const catalogs = uploadedCatalogs.map((catalog) => ({ id: catalog.id,
    category: "Katalog", eyebrow: "PDF KATALOĞU", title: catalog.title,
    description: catalog.fileName, image: catalog.coverUrl, pdfUrl: catalog.url }));

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
          <strong>{catalogs.length}</strong>
          <span>KATALOG</span>
          <div className={styles.heroLine} aria-hidden="true" />
          <p>PDF önizlemesi sayfadan ayrılmadan açılır.</p>
        </div>
      </section>

      <CatalogGallery catalogs={catalogs} />
    </main>
  );
}
