import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../../../components/public-header";
import styles from "../../endustriyel-mutfak/industrial.module.css";

export const metadata: Metadata = { title: "Industrial Kitchen | Ekiphan", description: "Professional equipment and project solutions for commercial kitchens.", alternates: { canonical: "/en/industrial-kitchen", languages: { tr: "/endustriyel-mutfak", en: "/en/industrial-kitchen" } } };
const categories = [["Preparation Equipment", "Worktables, sinks and preparation counters", "hazirlik"], ["Cooking Equipment", "Ranges, ovens, grills and cooking units", "pisirme"], ["Refrigeration", "Refrigerators, blast chillers and cold rooms", "sogutma"], ["Washing Systems", "Dishwashers, wash units and fittings", "yikama"], ["Storage Systems", "Wall cabinets, drawer units and shelving", "saklama"], ["Ventilation", "Hoods, flue systems and filtration solutions", "havalandirma"]] as const;
const benefits = [["01", "Project Consultancy", "Planning tailored to your space, capacity and operation."], ["02", "The Right Equipment", "A considered selection from leading brands."], ["03", "Installation & Commissioning", "Implementation, installation and commissioning support."], ["04", "After-Sales Support", "Maintenance, technical service and ongoing assistance."]] as const;

export default function EnglishIndustrialKitchenPage() {
  return <main className={styles.page} data-public-page><PublicHeader currentPath="/en/industrial-kitchen" />
    <section className={styles.hero}><div className={styles.heroCopy}><p>INDUSTRIAL KITCHEN</p><h1>Your trusted partner for professional kitchens.</h1><span>From planning and equipment selection to installation and after-sales support, we bring lasting value to professional kitchen projects.</span><div><Link href="/en/products">Explore Products</Link><Link href="/en/contact">Discuss Your Project</Link></div></div></section>
    <section className={styles.categories}><header><p>EQUIPMENT CATEGORIES</p></header><div className={styles.categoryGrid}>{categories.map(([title, text, key]) => <Link key={key} href={`/en/products?section=mutfak&category=${key}`} className={styles.categoryCard}><span className={`${styles.categoryPhoto} ${styles[key]}`} /><h3>{title}</h3><p>{text}</p><b>Explore category<i>→</i></b></Link>)}</div></section>
    <section className={styles.project}><div className={styles.projectImage} /><div><p>A PROJECT-LED APPROACH</p><span>We create professional kitchens tailored to your business through the right layout, equipment and operational flow.</span><Link href="/en/contact">Request a Quote<i>→</i></Link></div></section>
    <section className={styles.benefits}>{benefits.map(([number, title, text]) => <article key={number}><b>{number}</b><h3>{title}</h3><p>{text}</p></article>)}</section>
  </main>;
}
