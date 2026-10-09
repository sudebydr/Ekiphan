"use client";

import Link from "next/link";
import { useEffect, useRef, useState, type CSSProperties } from "react";
import type { CatalogNavigation } from "../lib/catalog-types";
import styles from "./products-menu.module.css";

type MenuBounds = {
  left: number;
  top: number;
  width: number;
  maxHeight: number;
};

export function ProductsCategoryBrowser({
  categories,
  locale,
}: {
  categories: CatalogNavigation["categories"];
  locale: "tr" | "en";
}) {
  const roots = categories.filter(item => !item.parentId);
  const base = locale === "en" ? "/en/products" : "/katalog";
  const ref = useRef<HTMLDivElement>(null);
  const [bounds, setBounds] = useState<MenuBounds | null>(null);

  useEffect(() => {
    const details = ref.current?.closest("details");
    if (!details) return;

    const update = () => {
      const summary = details.querySelector("summary");
      if (!summary) return;

      const rect = summary.getBoundingClientRect();
      const width = Math.min(360, window.innerWidth - 32);
      const left = Math.max(
        16,
        Math.min(rect.left, window.innerWidth - width - 16)
      );
      const top = Math.max(
        0,
        Math.min(rect.bottom, window.innerHeight - 80)
      );

      setBounds({
        left,
        top,
        width,
        maxHeight: Math.max(0, window.innerHeight - top - 16),
      });
    };

    update();
    details.addEventListener("toggle", update);
    window.addEventListener("resize", update);
    window.addEventListener("scroll", update, true);

    return () => {
      details.removeEventListener("toggle", update);
      window.removeEventListener("resize", update);
      window.removeEventListener("scroll", update, true);
    };
  }, []);

  const style = bounds
    ? {
        "--menu-left": `${bounds.left}px`,
        "--menu-top": `${bounds.top}px`,
        "--menu-width": `${bounds.width}px`,
        "--menu-height": `${bounds.maxHeight}px`,
      } as CSSProperties
    : undefined;

  return (
    <div ref={ref} className={`${styles.panel} ${styles.simplePanel}`} style={style}>
      <nav
        className={styles.simpleList}
        aria-label={locale === "en" ? "Product categories" : "Ürün kategorileri"}
      >
        {roots.map(category => (
          <Link
            key={category.id}
            href={`${base}?category=${encodeURIComponent(category.slug)}`}
            className={styles.simpleLink}
            prefetch={false}
          >
            {category.name}
          </Link>
        ))}
      </nav>

      <Link className={styles.footer} href={base}>
        {locale === "en" ? "All products" : "Tüm ürünler"} →
      </Link>
    </div>
  );
}