import styles from "./catalog-list.module.css";
import { PublicHeader } from "../../components/public-header";

export default function CatalogLoading() {
  return (
    <main className={styles.page} data-public-page aria-busy="true">
      <PublicHeader currentPath="/katalog" />
      <div className={styles.catalogLayout} aria-hidden="true">
        <aside className={styles.loadingFilters}>
          <span />
          <span />
          <span />
          <span />
          <span />
        </aside>
        <section className={styles.loadingProducts}>
          <div className={styles.loadingHeading} />
          <div className={styles.loadingGrid}>
            {Array.from({ length: 6 }, (_, index) => (
              <article className={styles.loadingCard} key={index}>
                <span />
                <i />
                <i />
              </article>
            ))}
          </div>
        </section>
      </div>
    </main>
  );
}
