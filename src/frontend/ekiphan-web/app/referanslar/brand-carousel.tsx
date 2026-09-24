"use client";

import { useRef } from "react";
import styles from "./references.module.css";

type Brand = { name: string; mark: string; accent?: boolean };

const featuredBrands: Brand[] = [
  { name: "Arçelik", mark: "arçelik", accent: true },
  { name: "Karaca", mark: "♜\nkaraca" },
  { name: "Schafer", mark: "SCHAFER\nMASTERS OF COOKING", accent: true },
  { name: "Goldmaster", mark: "GOLDMASTER" },
  { name: "Jumbo", mark: "Jumbo", accent: true },
  { name: "Bonna", mark: "bonna" },
  { name: "Paşabahçe", mark: "Paşabahçe", accent: true }
];


function BrandMark({ brand }: { brand: Brand }) { return <span className={`${styles.brandMark}${brand.accent ? ` ${styles.brandAccent}` : ""}`}>{brand.mark.split("\n").map((line) => <span key={line}>{line}</span>)}</span>; }

function Carousel({ brands, compact = false }: { brands: Brand[]; compact?: boolean }) {
  const track = useRef<HTMLDivElement>(null);
  const move = (direction: number) => track.current?.scrollBy({ left: direction * (compact ? 190 : 310), behavior: "smooth" });
  return <div className={compact ? styles.compactCarousel : styles.carousel}>
    <button className={styles.carouselArrow} type="button" aria-label="Önceki markalar" onClick={() => move(-1)}>‹</button>
    <div className={styles.carouselViewport} ref={track}>
      {brands.map((brand) => <article className={compact ? styles.compactBrandCard : styles.brandCard} key={brand.name}><BrandMark brand={brand} /></article>)}
    </div>
    <button className={styles.carouselArrow} type="button" aria-label="Sonraki markalar" onClick={() => move(1)}>›</button>
    {!compact && <div className={styles.dots} aria-hidden="true"><i className={styles.activeDot} /><i /><i /><i /><i /></div>}
  </div>;
}

export function BrandCarousels() { return <Carousel brands={featuredBrands} />; }