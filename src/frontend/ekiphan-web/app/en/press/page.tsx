import type { Metadata } from "next";
import { PublicHeader } from "../../../components/public-header";
import { getPressReleases } from "../../../lib/catalog-api";
import { PressShowcase } from "../../basin-odasi/press-showcase";
import styles from "../../basin-odasi/press.module.css";

export const dynamic = "force-dynamic";
export const metadata: Metadata = { title: "Press Room | Ekiphan", description: "The latest news and updates from Ekiphan.", alternates: { canonical: "/en/press", languages: { tr: "/basin-odasi", en: "/en/press" } } };
const demo = [{ id: "demo-1", category: "Company", date: "2026-08-12", title: "A renewed showroom experience awaits visitors.", summary: "Our showroom brings together professional kitchen, service and presentation spaces.", image: "/images/gallery/showroom-2026-08-11/TEK_9546%20copy.jpg", alt: "Professional kitchen at the Ekiphan showroom" }, { id: "demo-2", category: "Projects", date: "2026-07-28", title: "Planning sessions continue for project teams.", summary: "Our technical sessions focus on understanding the needs of each space.", image: "/images/gallery/showroom-2026-08-11/TEK_9628%20copy.jpg", alt: "Professional kitchen project detail" }];

export default async function EnglishPressPage() {
  let records: Awaited<ReturnType<typeof getPressReleases>> = [];
  try { records = await getPressReleases("en"); } catch { /* English demo items are shown when no translated records are available. */ }
  const items = records.length ? records.map((item) => ({ id: item.id, title: item.title, summary: item.summary, date: item.publishedAt, image: item.coverImageUrl ?? demo[0].image, alt: item.coverAltText ?? item.title, category: "News", href: item.attachmentUrl })) : demo;
  return <main className={styles.page} data-public-page><PublicHeader currentPath="/en/press" /><PressShowcase items={items} locale="en" /></main>;
}
