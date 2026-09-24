import type { Metadata } from "next";
import { PublicHeader } from "../../components/public-header";
import { getContentPage } from "../../lib/catalog-api";
import { managedMetadata } from "../../lib/managed-metadata";
import styles from "./references.module.css";

export const dynamic = "force-dynamic";

const references = [
  { name: "arcelik", accent: true }, { name: "VESTEL" }, { name: "divan" },
  { name: "RAMADA", sub: "BY WYNDHAM" }, { name: "MEDA", sub: "TOWER" },
  { name: "MEDICANA" }, { name: "DOGUS", sub: "GRUBU" }, { name: "TAHA", sub: "KARGO" },
  { name: "EGE", sub: "YAPI" }, { name: "Radisson" }, { name: "POLIMEKS" },
  { name: "ATASAY" }, { name: "FERKO" }, { name: "akasya" }, { name: "ISKELE", sub: "YAPI" }
];

export async function generateMetadata(): Promise<Metadata> {
  try { return managedMetadata(await getContentPage("referanslar"), "/referanslar"); }
  catch { return { title: "Referanslar", robots: { index: false, follow: false } }; }
}

export default function ReferencesPage() {
  return <main className={styles.page} data-public-page>
    <PublicHeader currentPath="/referanslar" />
    <section className={styles.references} aria-labelledby="references-title">
      <header className={styles.heading}>
        <h1 id="references-title">{"Referanslar\u0131m\u0131z"}</h1>
        <i aria-hidden="true" />
      </header>
      <div className={styles.brandGrid} aria-label="Referans markalar">
        {references.map((brand) => <article className={styles.brandCard} key={brand.name}>
          <span className={`${styles.brandMark}${brand.accent ? ` ${styles.accent}` : ""}`}>
            {brand.name}<small>{brand.sub}</small>
          </span>
        </article>)}
      </div>
    </section>
  </main>;
}