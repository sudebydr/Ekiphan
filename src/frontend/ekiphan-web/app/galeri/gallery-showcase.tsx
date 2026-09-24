"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import type { PublicGalleryItem } from "../../lib/gallery-types";
import styles from "./gallery.module.css";

type GalleryCard = PublicGalleryItem & { collection: string };
const allCollection = "T\u00fcm\u00fc";
const pageSize = 10;

export function GalleryShowcase({ items }: { items: GalleryCard[] }) {
  const [collection, setCollection] = useState(allCollection);
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const [visibleCount, setVisibleCount] = useState(pageSize);
  const touchStart = useRef<number | null>(null);
  const closeRef = useRef<HTMLButtonElement>(null);
  const loadMoreRef = useRef<HTMLDivElement>(null);
  const filters = useMemo(() => [allCollection, ...Array.from(new Set(items.map((item) => item.collection)))], [items]);
  const filtered = collection === allCollection ? items : items.filter((item) => item.collection === collection);
  const visible = filtered.slice(0, visibleCount);
  const selected = selectedIndex === null ? null : filtered[selectedIndex];

  function chooseCollection(next: string) {
    setCollection(next);
    setVisibleCount(pageSize);
    setSelectedIndex(null);
    const url = new URL(window.location.href);
    next === allCollection ? url.searchParams.delete("category") : url.searchParams.set("category", next);
    window.history.replaceState(null, "", url);
  }

  function move(direction: 1 | -1) {
    if (selectedIndex !== null && filtered.length > 1) {
      setSelectedIndex((selectedIndex + direction + filtered.length) % filtered.length);
    }
  }

  useEffect(() => {
    const requested = new URLSearchParams(window.location.search).get("category");
    if (requested && filters.includes(requested)) setCollection(requested);
  }, [filters]);

  useEffect(() => {
    const target = loadMoreRef.current;
    if (!target || visibleCount >= filtered.length) return;
    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) setVisibleCount((count) => Math.min(count + pageSize, filtered.length));
    }, { rootMargin: "360px 0px" });
    observer.observe(target);
    return () => observer.disconnect();
  }, [filtered.length, visibleCount]);

  useEffect(() => {
    if (!selected) return;
    const prior = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    closeRef.current?.focus();
    const handler = (event: KeyboardEvent) => {
      if (event.key === "Escape") setSelectedIndex(null);
      if (event.key === "ArrowLeft") move(-1);
      if (event.key === "ArrowRight") move(1);
    };
    window.addEventListener("keydown", handler);
    return () => {
      document.body.style.overflow = prior;
      window.removeEventListener("keydown", handler);
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedIndex, selected]);

  return <>
    <section id="projeler" className={styles.gallerySection} aria-label="Ekiphan g\u00f6rsel galerisi">
      <div className={styles.filterBar}>
        <div className={styles.filters} role="tablist" aria-label="Galeri kategorileri">
          {filters.map((filter) => <button key={filter} className={collection === filter ? styles.activeFilter : ""} onClick={() => chooseCollection(filter)} role="tab" aria-selected={collection === filter}>{filter}</button>)}
        </div>
        <p className={styles.itemCount}>{filtered.length}{" proje g\u00f6r\u00fcn\u00fcm\u00fc"}</p>
      </div>
      {visible.length ? <div className={styles.grid}>
        {visible.map((item, index) => <article className={`${styles.item} ${index % 7 === 0 ? styles.featured : ""} ${index % 7 === 4 ? styles.portrait : ""}`} key={item.id}>
          <button className={styles.visualButton} onClick={() => setSelectedIndex(index)} aria-label={`${item.title} g\u00f6rselini b\u00fcy\u00fct`}>
            <img src={item.imageUrl} alt={item.altText} loading={index < 2 ? "eager" : "lazy"} />
            <span className={styles.imageOverlay} aria-hidden="true"><span className={styles.collectionLabel}>{item.collection}</span><span className={styles.itemTitle}>{item.title}</span><span className={styles.expandIcon}>↗</span></span>
          </button>
          <div className={styles.mobileMeta}><span>{item.collection}</span><strong>{item.title}</strong>{item.caption && <small>{item.caption}</small>}</div>
        </article>)}
      </div> : <div className={styles.empty}><h2>Bu kategoride hen\u00fcz galeri i\u00e7eri\u011fi bulunmuyor.</h2><button onClick={() => chooseCollection(allCollection)}>T\u00fcm galeriyi g\u00f6r</button></div>}
      {visible.length < filtered.length && <div ref={loadMoreRef} className={styles.autoLoadMarker} aria-hidden="true" />}
    </section>
    {selected && <div className={styles.lightbox} role="dialog" aria-modal="true" aria-label={selected.title} onClick={() => setSelectedIndex(null)}>
      <div className={styles.lightboxTop}><span>{String((selectedIndex ?? 0) + 1).padStart(2, "0")} / {String(filtered.length).padStart(2, "0")}</span><button ref={closeRef} onClick={() => setSelectedIndex(null)} aria-label="Galeriyi kapat">×</button></div>
      <button className={`${styles.lightboxNav} ${styles.previous}`} onClick={(event) => { event.stopPropagation(); move(-1); }} aria-label="\u00d6nceki g\u00f6rsel">←</button>
      <figure onClick={(event) => event.stopPropagation()} onTouchStart={(event) => { touchStart.current = event.changedTouches[0]?.clientX ?? null; }} onTouchEnd={(event) => { const start = touchStart.current; const end = event.changedTouches[0]?.clientX; if (start !== null && end !== undefined && Math.abs(start - end) > 48) move(start > end ? 1 : -1); touchStart.current = null; }}><img src={selected.imageUrl} alt={selected.altText} /><figcaption><strong>{selected.title}</strong><span>{selected.collection}{selected.caption ? ` · ${selected.caption}` : ""}</span></figcaption></figure>
      <button className={`${styles.lightboxNav} ${styles.next}`} onClick={(event) => { event.stopPropagation(); move(1); }} aria-label="Sonraki g\u00f6rsel">→</button>
    </div>}
  </>;
}