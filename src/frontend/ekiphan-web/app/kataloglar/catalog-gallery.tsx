"use client";

import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
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

export function CatalogGallery({ catalogs, locale = "tr" }: { catalogs: Catalog[]; locale?: "tr" | "en" }) {
  const en = locale === "en";
  const [selected, setSelected] = useState<Catalog | null>(null);
  const closeButton = useRef<HTMLButtonElement>(null);
  const [page, setPage] = useState(0);
  const [failedCovers, setFailedCovers] = useState<Set<string>>(() => new Set());
  const totalPages = Math.ceil(catalogs.length / PAGE_SIZE);
  const visibleCatalogs = catalogs.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE);

  useEffect(() => {
    if (!selected) return;
    const previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const { scrollX, scrollY } = window;
    const body = document.body;
    const previous = { position: body.style.position, top: body.style.top, left: body.style.left,
      width: body.style.width, overflow: body.style.overflow };
    Object.assign(body.style, { position: "fixed", top: `-${scrollY}px`, left: `-${scrollX}px`, width: "100%", overflow: "hidden" });
    closeButton.current?.focus({ preventScroll: true });
    const close = (event: KeyboardEvent) => {
      if (event.key === "Escape") setSelected(null);
    };
    window.addEventListener("keydown", close);
    return () => {
      window.removeEventListener("keydown", close);
      Object.assign(body.style, previous);
      window.scrollTo({ left: scrollX, top: scrollY, behavior: "instant" });
      previousFocus?.focus({ preventScroll: true });
    };
  }, [selected]);

  const changePage = (nextPage: number) => {
    setPage(nextPage);
    document.getElementById("catalog-grid")?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  return (
    <>
      <section className={styles.catalogSection} aria-label={en ? "Catalogues" : "Kataloglar"}>
        <div className={styles.sectionIntro}>
          <p>{en ? "Catalogues" : "Kataloglar"}</p>
          <span>{en ? "Choose a catalogue to preview the PDF without leaving this page." : "Kataloğu seçin; PDF önizlemesi sayfadan ayrılmadan açılır."}</span>
        </div>
        <div className={styles.catalogGrid} id="catalog-grid">
          {visibleCatalogs.map((catalog, index) => (
            <button className={styles.catalogCard} key={catalog.id} onClick={() => setSelected(catalog)} type="button">
              <span className={styles.cover} aria-hidden="true">
                {!catalog.image || catalog.image === "/images/catalog-placeholder.webp" || failedCovers.has(catalog.image)
                  ? <span className={styles.coverPlaceholder}>PDF</span>
                  : <img src={catalog.image} alt="" onError={() => setFailedCovers(previous => new Set(previous).add(catalog.image))} />}
              </span>
              <span className={styles.cardOverlay} aria-hidden="true" />
              <span className={styles.cardContent}>
                <small>{catalog.eyebrow}</small>
                <strong>{catalog.title}</strong>
                <em>{catalog.description}</em>
                <b>{en ? "View PDF" : "PDF'i Görüntüle"}</b>
              </span>
              <span className={styles.index} aria-hidden="true">{String(page * PAGE_SIZE + index + 1).padStart(2, "0")}</span>
            </button>
          ))}
        </div>
        {totalPages > 1 && (
          <nav className={styles.pagination} aria-label="Katalog sayfaları">
            <button type="button" onClick={() => changePage(page - 1)} disabled={page === 0} aria-label={en ? "Previous page" : "Önceki sayfa"}>←</button>
            {Array.from({ length: totalPages }, (_, index) => (
              <button key={index} type="button" onClick={() => changePage(index)} className={index === page ? styles.activePage : ""} aria-current={index === page ? "page" : undefined}>{index + 1}</button>
            ))}
            <button type="button" onClick={() => changePage(page + 1)} disabled={page === totalPages - 1} aria-label={en ? "Next page" : "Sonraki sayfa"}>→</button>
          </nav>
        )}
      </section>

      {selected && createPortal(
        <div className={styles.modal} role="dialog" aria-modal="true" aria-label={en ? `${selected.title} PDF preview` : `${selected.title} PDF önizlemesi`} onMouseDown={() => setSelected(null)}>
          <section className={styles.modalPanel} onMouseDown={(event) => event.stopPropagation()}>
            <header className={styles.viewerHeader}>
              <div className={styles.modalMeta}><p>{selected.category} · {selected.eyebrow}</p><h2>{selected.title}</h2></div>
              <button ref={closeButton} className={styles.close} type="button" onClick={() => setSelected(null)} aria-label={en ? "Close preview" : "Önizlemeyi kapat"}>{en ? "Close" : "Kapat"} <span>×</span></button>
            </header>
            <div className={styles.viewerSurface}>
              <iframe className={styles.pdfFrame} src={`${encodeURI(selected.pdfUrl)}#page=1&zoom=80&pagemode=none`} title={`${selected.title} PDF kataloğu`} />
            </div>
          </section>
        </div>, document.body
      )}
    </>
  );
}
