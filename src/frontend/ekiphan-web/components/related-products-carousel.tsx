"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import styles from "../app/katalog/product-detail.module.css";

type Item = { id: string; slug: string; name: string; sku: string; category: string; brand: string; image?: { url: string; altText: string } | null };

function visibleCards(width: number) { return width < 768 ? 1 : width < 1200 ? 2 : 3; }

export function RelatedProductsCarousel({ items, locale = "tr" }: { items: Item[]; locale?: "tr" | "en" }) {
  const en = locale === "en";
  const viewportRef = useRef<HTMLDivElement>(null);
  const touchStart = useRef<number | null>(null);
  const [index, setIndex] = useState(0);
  const [perView, setPerView] = useState(3);
  const [step, setStep] = useState(0);
  const maxIndex = Math.max(0, items.length - perView);
  const move = (direction: 1 | -1) => setIndex((current) => Math.max(0, Math.min(maxIndex, current + direction)));

  useEffect(() => {
    const viewport = viewportRef.current;
    if (!viewport) return;
    const update = () => {
      const nextPerView = visibleCards(viewport.clientWidth);
      setPerView(nextPerView);
      setStep((viewport.clientWidth - (nextPerView - 1) * 24) / nextPerView + 24);
    };
    update();
    const observer = new ResizeObserver(update);
    observer.observe(viewport);
    return () => observer.disconnect();
  }, []);

  useEffect(() => setIndex((current) => Math.min(current, Math.max(0, items.length - perView))), [items.length, perView]);
  if (!items.length) return null;

  return <div className={styles.relatedCarousel}>
    <button type="button" className={`${styles.relatedArrow} ${styles.relatedPrev}`} onClick={() => move(-1)} disabled={index === 0} aria-label={en ? "Previous products" : "Önceki ürünler"}>←</button>
    <div className={styles.relatedViewport} ref={viewportRef} onTouchStart={(event) => { touchStart.current = event.touches[0]?.clientX ?? null; }} onTouchEnd={(event) => { const start = touchStart.current; const end = event.changedTouches[0]?.clientX; if (start !== null && end !== undefined && Math.abs(start - end) > 42) move(start > end ? 1 : -1); touchStart.current = null; }}>
      <div className={styles.relatedTrack} style={{ transform: `translateX(-${index * step}px)` }}>
        {items.map((product) => <Link className={styles.relatedCard} href={`${en ? "/en/products" : "/katalog"}/${product.slug}`} key={product.id}>
          <span className={styles.relatedVisual}>{product.image ? <img src={product.image.url} alt={product.image.altText} width={560} height={560} loading="lazy" /> : product.name.slice(0, 1)}</span>
          <span className={styles.relatedCopy}><small>{product.category}</small><span>{product.brand}</span><strong>{product.name}</strong><em>{en ? "Model" : "Model"} {product.sku}</em><b>{en ? "View Product" : "Ürünü incele"} <i aria-hidden="true">→</i></b></span>
        </Link>)}
      </div>
    </div>
    <button type="button" className={`${styles.relatedArrow} ${styles.relatedNext}`} onClick={() => move(1)} disabled={index === maxIndex} aria-label={en ? "Next products" : "Sonraki ürünler"}>→</button>
    {maxIndex > 0 && <div className={styles.relatedDots} aria-label={en ? "Product carousel pages" : "Ürün carousel sayfaları"}>{Array.from({ length: maxIndex + 1 }, (_, dot) => <button key={dot} type="button" className={dot === index ? styles.relatedDotActive : ""} onClick={() => setIndex(dot)} aria-label={en ? `Show product group ${dot + 1}` : `${dot + 1}. ürün grubunu göster`} aria-current={dot === index} />)}</div>}
  </div>;
}
