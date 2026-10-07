import Link from "next/link";
import styles from "./press.module.css";

type PressItem = { id: string; title: string; summary: string; date: string; image: string; alt: string; category: string; href?: string | null };

const formatDate = (value: string) => new Intl.DateTimeFormat("tr-TR", { day: "numeric", month: "long", year: "numeric" }).format(new Date(value));


export function PressShowcase({ items, locale = "tr" }: { items: PressItem[]; locale?: "tr" | "en" }) {
  const en = locale === "en";
  return <section className={styles.pressRoom} aria-labelledby="press-title">
    <header className={styles.pageHeading}>
      <div><p>{en ? "Press Room" : "Basın Odası"}</p><h1 id="press-title">{en ? "Latest" : "En Yeni"}</h1></div>
    </header>
    <div className={styles.layout}>
      <div className={styles.newsList} id="tum-haberler">
        {items.slice(0, 3).map((item) => <article className={styles.newsCard} key={item.id}>
          <img src={item.image} alt={item.alt} width={900} height={560} />
          <div className={styles.newsContent}>
            <span>{item.category}</span><h2>{item.title}</h2><p>{item.summary}</p>
            <footer><time dateTime={item.date}>{new Intl.DateTimeFormat(en ? "en-GB" : "tr-TR", { day: "numeric", month: "long", year: "numeric" }).format(new Date(item.date))}</time>{item.href && <a href={item.href}>{en ? "Read story" : "Haberi aç"} <b aria-hidden="true">→</b></a>}</footer>
          </div>
        </article>)}
      </div>
    </div>
  </section>;
}
