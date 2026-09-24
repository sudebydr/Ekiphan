import type { Metadata } from "next";
import { PublicHeader } from "../../components/public-header";
import { getContentPage, getGallery } from "../../lib/catalog-api";
import type { PublicGalleryItem } from "../../lib/gallery-types";
import { managedMetadata } from "../../lib/managed-metadata";
import styles from "./gallery.module.css";
import { GalleryShowcase } from "./gallery-showcase";

export const dynamic = "force-dynamic";

const showroomFiles = ["TEK_9403 copy.jpg", "TEK_9546 copy.jpg", "TEK_9628 copy.jpg", "TEK_9560 copy.jpg", "TEK_9664 copy.jpg", "TEK_9549 copy.jpg", "TEK_9562 copy.jpg", "TEK_9586.jpg", "1_1.jpg", "TEK_9502 copy.jpg", "TEK_9554 copy.jpg", "5.jpg", "TEK_9548 copy.jpg", "TEK_1121 copy.jpg", "TEK_9500 copy.jpg", "TEK_9412 copy.jpg", "TEK_9604 copy.jpg", "TEK_9569 copy.jpg", "TEK_9556 copy.jpg", "20.jpg", "TEK_9587.jpg", "TEK_9407 copy.jpg", "TEK_9503 copy.jpg", "4.jpg", "16.jpg", "TEK_9543 copy.jpg", "TEK_9555 copy.jpg", "25.jpg", "1.jpg", "TEK_9551 copy.jpg"];

const showroomItems = showroomFiles.map((file, index) => ({
  id: `showroom-2026-08-11-${index + 1}`,
  title: `Showroom se\u00e7kisi ${String(index + 1).padStart(2, "0")}`,
  caption: "\u0130stanbul / T\u00fcrkiye",
  imageUrl: `/images/gallery/showroom-2026-08-11/${encodeURIComponent(file)}`,
  altText: `Ekiphan showroomundan profesyonel mutfak g\u00f6r\u00fcn\u00fcm\u00fc ${index + 1}`,
  collection: "Showroom"
}));

export async function generateMetadata(): Promise<Metadata> {
  try { return managedMetadata(await getContentPage("galeri"), "/galeri"); }
  catch { return { title: "Galeri", robots: { index: false, follow: false } }; }
}

export default async function GalleryPage() {
  let items: PublicGalleryItem[] = [];
  try { items = await getGallery(); } catch { /* Local fallback remains available. */ }
  const displayItems = [...showroomItems, ...items.map((item) => ({ ...item, collection: "Kurumsal" }))];

  return <main className={styles.page} data-public-page>
    <PublicHeader currentPath="/galeri" />
    <div className={styles.content}>
      <nav className={styles.breadcrumb} aria-label="Sayfa yolu"><a href="/">Ana Sayfa</a><span>{"\u203a"}</span><strong>Galeri</strong></nav>
      <section className={styles.hero}>
        <div className={styles.heroCopy}>
          <p>PROJELER &amp; SHOWROOM</p>
          <h1>Projelerden<br />{"\u0130lham Alan \u00c7\u00f6z\u00fcmler"}</h1>
          <span>{"Otel, restoran, kafe ve end\u00fcstriyel mutfak projelerimizden se\u00e7kiyi ke\u015ffedin. Her proje, profesyonel mutfaklara \u00f6zel \u00e7\u00f6z\u00fcmlerimizin bir yans\u0131mas\u0131d\u0131r."}</span>
          <a href="#projeler">{"Projelerimizi \u0130ncele"} <b aria-hidden="true">{"\u2192"}</b></a>
        </div>
      </section>
    </div>
    <GalleryShowcase items={displayItems} />
  </main>;
}