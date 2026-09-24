import type { Metadata } from "next";
import { PublicHeader } from "../../components/public-header";
import { CatalogGallery } from "./catalog-gallery";
import styles from "./catalogs.module.css";

export const metadata: Metadata = {
  title: "Kataloglar | Ekiphan",
  description: "Ekiphan profesyonel mutfak \u00e7\u00f6z\u00fcmleri kataloglar\u0131."
};

const catalogs = [
  { id: "acik-bufe", category: "Ekiphan kataloglar\u0131", eyebrow: "EK\u0130PHAN \u00b7 2026", title: "Ekiphan a\u00e7\u0131k b\u00fcfe", description: "A\u00e7\u0131k b\u00fcfe sunum \u00e7\u00f6z\u00fcmleri.", image: "/images/catalog-product-worlds-ai.png", pdfUrl: "/catalogs/Ekiphan-açık büfe.pdf" },
  { id: "bar", category: "Ekiphan kataloglar\u0131", eyebrow: "EK\u0130PHAN \u00b7 2026", title: "Ekiphan bar", description: "Bar ve servis alan\u0131 se\u00e7kileri.", image: "/images/catalog-products-hero-v2.png", pdfUrl: "/catalogs/Ekiphan-bar.pdf" },
  { id: "cihazlar", category: "Ekiphan kataloglar\u0131", eyebrow: "EK\u0130PHAN \u00b7 2026", title: "Ekiphan cihazlar", description: "Profesyonel mutfak ekipmanlar\u0131.", image: "/images/industrial-kitchen-premium.png", pdfUrl: "/catalogs/Ekiphan-cihazlar.pdf" },
  { id: "restaurant", category: "Ekiphan kataloglar\u0131", eyebrow: "EK\u0130PHAN \u00b7 2026", title: "Restaurant malzemeleri", description: "Restoran kullan\u0131m\u0131na y\u00f6nelik \u00fcr\u00fcn se\u00e7kileri.", image: "/images/feature-selection.png", pdfUrl: "/catalogs/Ekiphan_Restaurant Malzemeleri.pdf" },
  { id: "klasik", category: "Ekiphan kataloglar\u0131", eyebrow: "EK\u0130PHAN \u00b7 2026", title: "Klasik se\u00e7ki", description: "Zamans\u0131z servis ve sunum \u00fcr\u00fcnleri.", image: "/images/catalog-product-worlds-ai.png", pdfUrl: "/catalogs/Ekiphan_catal_kasik_2026.pdf" },
  { id: "kutahya", category: "Marka kataloglar\u0131", eyebrow: "MARKA KATALO\u011eU \u00b7 2026", title: "K\u00fctahya Porselen", description: "Porselen \u00fcr\u00fcn se\u00e7kileri.", image: "/images/feature-selection.png", pdfUrl: "/catalogs/Kütahya-Porselen-2026.pdf" },
  { id: "bonna", category: "Marka kataloglar\u0131", eyebrow: "MARKA KATALO\u011eU \u00b7 2026", title: "Bonna", description: "Profesyonel porselen koleksiyonu.", image: "/images/catalog-products-hero-v2.png", pdfUrl: "/catalogs/bonna-katalog-2026.pdf" },
  { id: "esma", category: "Marka kataloglar\u0131", eyebrow: "MARKA KATALO\u011eU \u00b7 2026", title: "Esmadereboy", description: "Masa \u00fcst\u00fc \u00fcr\u00fcn se\u00e7kileri.", image: "/images/catalog-product-worlds-ai.png", pdfUrl: "/catalogs/esmadereboy-2026.pdf" },
  { id: "fabrika", category: "Marka kataloglar\u0131", eyebrow: "MARKA KATALO\u011eU \u00b7 2026", title: "Fabrika", description: "Mutfak \u00e7\u00f6z\u00fcmleri katalo\u011fu.", image: "/images/industrial-kitchen-premium.png", pdfUrl: "/catalogs/fabrika-2026.pdf" },
  { id: "nude", category: "Marka kataloglar\u0131", eyebrow: "MARKA KATALO\u011eU \u00b7 2026", title: "Nude", description: "Cam ve sunum \u00fcr\u00fcnleri.", image: "/images/feature-selection.png", pdfUrl: "/catalogs/nude-2026.pdf" },
  { id: "pasabahce", category: "Marka kataloglar\u0131", eyebrow: "MARKA KATALO\u011eU \u00b7 2026", title: "Pa\u015fabah\u00e7e", description: "Servis ve bardak koleksiyonu.", image: "/images/catalog-products-hero-v2.png", pdfUrl: "/catalogs/paşabahçe-2026.pdf" },
  { id: "selene", category: "Marka kataloglar\u0131", eyebrow: "MARKA KATALO\u011eU \u00b7 2026", title: "Selene", description: "Se\u00e7ili \u00fcr\u00fcn kataloglar\u0131.", image: "/images/catalog-product-worlds-ai.png", pdfUrl: "/catalogs/selene_11.06.26.pdf" }
];

export default function CatalogsPage() {
  return (
    <main className={styles.page} data-public-page>
      <PublicHeader currentPath="/kataloglar" />
      <section className={styles.hero}>
        <h1>{"\u0130lham veren \u00e7\u00f6z\u00fcmleri"}<br />{"yak\u0131ndan inceleyin."}</h1>
        <p>{"\u00dcr\u00fcn se\u00e7kilerini ayn\u0131 sayfada a\u00e7\u0131n, detaylar\u0131 kesintisiz \u015fekilde ke\u015ffedin."}</p>
      </section>
      <CatalogGallery catalogs={catalogs} />
    </main>
  );
}
