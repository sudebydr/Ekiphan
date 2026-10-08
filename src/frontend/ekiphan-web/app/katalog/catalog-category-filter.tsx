"use client";

import Link from "next/link";
import type { CatalogNavigation } from "../../lib/catalog-types";
import { optionHref } from "./catalog-filter-links";
import styles from "./catalog-list.module.css";

type Category = CatalogNavigation["categories"][number];

export function CatalogCategoryFilter({ categories, selectedSlug, baseQuery, isEnglish = false }: {
  categories: Category[]; selectedSlug: string; baseQuery: string; isEnglish?: boolean;
}) {
  const selected = categories.find(item => item.slug === selectedSlug);
  const ancestors = new Set<string>();
  let current = selected;
  while (current && !ancestors.has(current.id)) {
    ancestors.add(current.id);
    const parentId = current.parentId;
    current = categories.find(item => item.id === parentId);
  }
  const children = new Map<string | null, Category[]>();
  for (const item of categories) children.set(item.parentId, [...(children.get(item.parentId) ?? []), item]);
  function branch(parent: string | null, depth = 0): React.ReactNode {
    if (depth > categories.length) return null;
    return (children.get(parent) ?? []).map(item => {
      const link = <Link className={`${styles.option} ${item.slug === selectedSlug ? styles.optionSelected : ""}`} href={optionHref(baseQuery, "category", item.slug)} aria-current={item.slug === selectedSlug ? "true" : undefined} prefetch={false} scroll={false}>{item.name}</Link>;
      return <li key={item.id}>{children.has(item.id)
        ? <details open={ancestors.has(item.id)}><summary className={styles.categoryRow}>{link}<span aria-hidden="true">＋</span></summary><ul className={styles.subList}>{branch(item.id, depth + 1)}</ul></details>
        : link}</li>;
    });
  }
  return <details className={styles.filterGroup} open={Boolean(selectedSlug)}>
    <summary><span>{isEnglish ? "Categories" : "Kategoriler"}</span>{selected && <span className={styles.filterCurrent}>{selected.name}</span>}</summary>
    <ul className={styles.optionList}><li><Link className={styles.option} href={optionHref(baseQuery, "category", "")} prefetch={false}>{isEnglish ? "All categories" : "Tüm kategoriler"}</Link></li>{branch(null)}</ul>
  </details>;
}
