"use client";

import Link from "next/link";
import { useEffect, useRef, useState, type CSSProperties, type KeyboardEvent } from "react";
import type { CatalogNavigation } from "../lib/catalog-types";
import { categoryPath, createHoverIntent, menuBounds } from "./products-menu-layout.mjs";
import styles from "./products-menu.module.css";

export function ProductsCategoryBrowser({ categories, locale }: { categories: CatalogNavigation["categories"]; locale: "tr" | "en" }) {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [bounds, setBounds] = useState<ReturnType<typeof menuBounds> | null>(null);
  const ref = useRef<HTMLDivElement>(null);
  const [hover] = useState(() => createHoverIntent(setSelectedId));
  useEffect(() => () => hover.cancel(), [hover]);
  const roots = categories.filter(item => !item.parentId);
  const path = categoryPath(categories, selectedId);
  const selected = path.at(-1);
  const children = categories.filter(item => item.parentId === selectedId);
  const en = locale === "en";
  const base = en ? "/en/products" : "/katalog";
  const href = (slug: string) => `${base}?category=${encodeURIComponent(slug)}`;

  useEffect(() => {
    const details = ref.current?.closest("details");
    if (!details) return;
    const update = () => {
      const rect = details.querySelector("summary")!.getBoundingClientRect();
      setBounds(menuBounds(window.innerWidth, window.innerHeight, rect.left, rect.bottom));
    };
    const toggle = () => { update(); if (!details.open) { hover.cancel(); setSelectedId(null); } };
    update();
    details.addEventListener("toggle", toggle);
    window.addEventListener("resize", update);
    window.addEventListener("scroll", update, true);
    return () => { details.removeEventListener("toggle", toggle); window.removeEventListener("resize", update); window.removeEventListener("scroll", update, true); };
  }, [hover]);

  function navigate(event: KeyboardEvent<HTMLElement>) {
    if (!["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) return;
    const controls = Array.from(event.currentTarget.querySelectorAll<HTMLElement>("a,button"));
    const index = controls.indexOf(document.activeElement as HTMLElement);
    const next = event.key === "Home" ? 0 : event.key === "End" ? controls.length - 1 : (index + (event.key === "ArrowDown" ? 1 : -1) + controls.length) % controls.length;
    event.preventDefault(); event.stopPropagation(); controls[next]?.focus();
  }

  const branch = (desktop = false) => selected && <div className={styles.branch} onKeyDown={navigate}>
    <div className={styles.branchHeader}>
      <button type="button" onClick={() => setSelectedId(selected.parentId)}>{en ? "← Back" : "← Geri"}</button>
      <Link href={href(selected.slug)} prefetch={false}>{selected.name} — {en ? "View all" : "Tümünü gör"}</Link>
    </div>
    <ul className={styles.childList}>{children.map(child => <li key={child.id}
      onPointerMove={desktop ? event => { if (event.pointerType === "mouse" && window.matchMedia("(min-width: 901px)").matches && categories.some(item => item.parentId === child.id)) hover.schedule(child.id); } : undefined}
      onPointerLeave={desktop ? () => hover.cancel() : undefined}>
      <Link href={href(child.slug)} prefetch={false} onKeyDown={desktop ? event => { if (event.key === "ArrowRight" && categories.some(item => item.parentId === child.id)) { event.preventDefault(); hover.cancel(); setSelectedId(child.id); } } : undefined}>{child.name}</Link>
      {categories.some(item => item.parentId === child.id) && <button type="button" onClick={() => setSelectedId(child.id)} aria-label={`${child.name}: ${en ? "subcategories" : "alt kategoriler"}`}>→</button>}
    </li>)}</ul>
  </div>;

  return <div ref={ref} className={styles.panel} onPointerLeave={() => hover.cancel()} style={bounds ? { "--menu-left": `${bounds.left}px`, "--menu-top": `${bounds.top}px`, "--menu-width": `${bounds.width}px`, "--menu-height": `${bounds.maxHeight}px` } as CSSProperties : undefined}>
    <div className={styles.desktop} data-selected={Boolean(selected)}>
      <div className={styles.roots} onKeyDown={navigate} aria-label={en ? "Product categories" : "Ürün kategorileri"}>
        {roots.map(root => <Link key={root.id} href={href(root.slug)} prefetch={false}
          aria-current={path[0]?.id === root.id ? "true" : undefined}
          onPointerEnter={event => { if (event.pointerType === "mouse" && window.matchMedia("(min-width: 901px)").matches) { if (categories.some(item => item.parentId === root.id)) hover.schedule(root.id); else hover.cancel(); } }}
          onPointerLeave={() => hover.cancel()}
          onKeyDown={event => { if (event.key === "ArrowRight" && categories.some(item => item.parentId === root.id)) { event.preventDefault(); hover.cancel(); setSelectedId(root.id); } }}>
          {root.name}<span aria-hidden="true">{categories.some(item => item.parentId === root.id) ? "→" : "↗"}</span>
        </Link>)}
      </div>
      {branch(true) || <p className={styles.desktopHint}>{en ? "Select a category to explore its products." : "Ürünleri keşfetmek için bir kategori seçin."}</p>}
    </div>
    <div className={styles.mobile} onKeyDown={navigate}>
      {roots.map(root => <div key={root.id} className={styles.accordion}>
        <button type="button" className={styles.rootToggle} aria-expanded={path[0]?.id === root.id} onClick={() => setSelectedId(path[0]?.id === root.id ? null : root.id)}>{root.name}<span aria-hidden="true">{path[0]?.id === root.id ? "−" : "+"}</span></button>
        {path[0]?.id === root.id && branch()}
      </div>)}
    </div>
    <Link className={styles.footer} href={base}>{en ? "All products" : "Tüm ürünler"} →</Link>
  </div>;
}
