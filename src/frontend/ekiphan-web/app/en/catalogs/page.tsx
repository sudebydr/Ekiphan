import type { Metadata } from "next";
import { PublicHeader } from "../../../components/public-header";
import { CatalogGallery } from "../../kataloglar/catalog-gallery";
import { getCatalogPdfDocuments } from "../../../lib/catalog-api";
import styles from "../../kataloglar/catalogs.module.css";

export const dynamic = "force-dynamic";
export const metadata: Metadata = { title: "Catalogues | Ekiphan", description: "Explore Ekiphan's professional kitchen solutions catalogues.", alternates: { canonical: "/en/catalogs", languages: { tr: "/kataloglar", en: "/en/catalogs" } } };

export default async function EnglishCatalogsPage() {
  const docs = await getCatalogPdfDocuments();
  const catalogs = docs.map((item) => ({ id: item.id, category: "Catalogue", eyebrow: "PDF CATALOGUE", title: item.title, description: item.fileName, image: item.coverUrl, pdfUrl: item.url }));
  return <main className={styles.page} data-public-page><PublicHeader currentPath="/en/catalogs" />
    <section className={styles.hero} aria-labelledby="catalogs-title"><div className={styles.heroIndex} aria-hidden="true">01</div><div className={styles.heroCopy}><div className={styles.heroEyebrow}><span>CATALOGUE ARCHIVE</span><i aria-hidden="true" /><span>EKIPHAN</span></div><h1 id="catalogs-title">Inspiring<em>collections.</em></h1><p>Explore the catalogues we have prepared for professional kitchens, service areas and brand collections.</p></div><div className={styles.heroSide}><strong>{catalogs.length}</strong><span>CATALOGUES</span><div className={styles.heroLine} aria-hidden="true" /><p>Preview PDFs without leaving the page.</p></div></section>
    <CatalogGallery catalogs={catalogs} locale="en" />
  </main>;
}
