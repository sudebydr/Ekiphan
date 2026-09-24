"use client";

import { useEffect, useState } from "react";
import styles from "./catalogs.module.css";

type Catalog = {
  id: string;
  category: string;
  eyebrow: string;
  title: string;
  description: string;
  image: string;
  pdfUrl: string;
};

const PAGE_SIZE = 8;

export function CatalogGallery({ catalogs }: { catalogs: Catalog[] }) {
  const [selected, setSelected] = useState<Catalog | null>(null);
  const [page, setPage] = useState(0);
  const totalPages = Math.ceil(catalogs.length / PAGE_SIZE);
  const visibleCatalogs = catalogs.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE);

  useEffect(() => {
    const close = (event: KeyboardEvent) => {
      if (event.key === "Escape") setSelected(null);
    };
    window.addEventListener("keydown", close);
    return () => window.removeEventListener("keydown", close);
  }, []);

  const changePage = (nextPage: number) => {
    setPage(nextPage);
    document.getElementById("catalog-grid")?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  return (
    <>
      <section className={styles.catalogSection} aria-label="Kataloglar">
        <div className={styles.sectionIntro}>
          <p>Kataloglar</p>
          <span>Kataloğu seçin; PDF önizlemesi sayfadan ayrılmadan açılır.</span>
        </div>
        <div className={styles.catalogGrid} id="catalog-grid">
          {visibleCatalogs.map((catalog, index) => (
            <button className={styles.catalogCard} key={catalog.id} onClick={() => setSelected(catalog)} type="button">
              <span className={styles.cover} style={{ backgroundImage: `url(${catalog.image})` }} aria-hidden="true" />
              <span className={styles.cardOverlay} aria-hidden="true" />
              <span className={styles.cardContent}>
                <small>{catalog.eyebrow}</small>
                <strong>{catalog.title}</strong>
                <em>{catalog.description}</em>
                <b>PDF&apos;i Görüntüle</b>
              </span>
              <span className={styles.index} aria-hidden="true">{String(page * PAGE_SIZE + index + 1).padStart(2, "0")}</span>
            </button>
          ))}
        </div>
        {totalPages > 1 && (
          <nav className={styles.pagination} aria-label="Katalog sayfaları">
            <button type="button" onClick={() => changePage(page - 1)} disabled={page === 0} aria-label="Önceki sayfa">←</button>
            {Array.from({ length: totalPages }, (_, index) => (
              <button key={index} type="button" onClick={() => changePage(index)} className={index === page ? styles.activePage : ""} aria-current={index === page ? "page" : undefined}>{index + 1}</button>
            ))}
            <button type="button" onClick={() => changePage(page + 1)} disabled={page === totalPages - 1} aria-label="Sonraki sayfa">→</button>
          </nav>
        )}
      </section>

      {selected && (
        <div className={styles.modal} role="dialog" aria-modal="true" aria-label={`${selected.title} PDF önizlemesi`} onMouseDown={() => setSelected(null)}>
          <section className={styles.modalPanel} onMouseDown={(event) => event.stopPropagation()}>
            <header className={styles.viewerHeader}>
              <div className={styles.modalMeta}><p>{selected.category} · {selected.eyebrow}</p><h2>{selected.title}</h2></div>
              <button className={styles.close} type="button" onClick={() => setSelected(null)} aria-label="Önizlemeyi kapat">Kapat <span>×</span></button>
            </header>
            <div className={styles.viewerSurface}>
              <iframe className={styles.pdfFrame} src={`${encodeURI(selected.pdfUrl)}#page=1&zoom=80&pagemode=none`} title={`${selected.title} PDF kataloğu`} />
            </div>
          </section>
        </div>
      )}
    </>
  );
}
