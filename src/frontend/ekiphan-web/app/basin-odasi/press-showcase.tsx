import Link from "next/link";
import styles from "./press.module.css";

type PressItem = { id: string; title: string; summary: string; date: string; image: string; alt: string; category: string; href?: string | null };

const formatDate = (value: string) => new Intl.DateTimeFormat("tr-TR", { day: "numeric", month: "long", year: "numeric" }).format(new Date(value));


export function PressShowcase({ items }: { items: PressItem[] }) {
  return <section className={styles.pressRoom} aria-labelledby="press-title">
    <header className={styles.pageHeading}>
      <div><p>Basın Odası</p><h1 id="press-title">En Yeni</h1></div>
    </header>
    <div className={styles.layout}>
      <div className={styles.newsList} id="tum-haberler">
        {items.slice(0, 3).map((item) => <article className={styles.newsCard} key={item.id}>
          <img src={item.image} alt={item.alt} width={900} height={560} />
          <div className={styles.newsContent}>
            <span>{item.category}</span><h2>{item.title}</h2><p>{item.summary}</p>
            <footer><time dateTime={item.date}>{formatDate(item.date)}</time>{item.href && <a href={item.href}>Haberi aç <b aria-hidden="true">→</b></a>}</footer>
          </div>
        </article>)}
      </div>
    </div>
  </section>;
}