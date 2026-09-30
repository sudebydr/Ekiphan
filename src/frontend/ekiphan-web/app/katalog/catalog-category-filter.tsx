"use client";

import Link from "next/link";
import { useState } from "react";
import type { CatalogNavigation } from "../../lib/catalog-types";
import styles from "./catalog.module.css";

type Category = CatalogNavigation["categories"][number];

export function CatalogCategoryFilter({ categories, selectedSlug }: { categories: Category[]; selectedSlug: string }) {
  const roots = categories.filter((item) => item.parentId === null);
  const children = new Map<string, Category[]>();
  for (const item of categories) if (item.parentId) children.set(item.parentId, [...(children.get(item.parentId) ?? []), item]);
  const [open, setOpen] = useState(() => new Set(roots.filter(root => root.slug === selectedSlug || children.get(root.id)?.some(child => child.slug === selectedSlug)).map(root => root.id)));
  const href = (slug?: string) => slug ? `/katalog?category=${encodeURIComponent(slug)}` : "/katalog";
  return <nav className={styles.categoryFilter} aria-label="Kategoriler">
    <h3>Kategoriler</h3>
    <Link className={!selectedSlug ? styles.categorySelected : ""} href={href()}>Tüm kategoriler</Link>
    {roots.map(root => {
      const groups = children.get(root.id) ?? [];
      const expanded = open.has(root.id);
      return <div key={root.id} className={styles.categoryRoot}>
        <div><Link className={selectedSlug === root.slug ? styles.categorySelected : ""} href={href(root.slug)}>{root.name}</Link>{groups.length > 0 && <button type="button" aria-expanded={expanded} aria-controls={`category-${root.id}`} onClick={() => setOpen(current => { const next = new Set(current); expanded ? next.delete(root.id) : next.add(root.id); return next; })}>{expanded ? "⌄" : "›"}</button>}</div>
        {groups.length > 0 && expanded && <div id={`category-${root.id}`} className={styles.categoryGroups}>{groups.map(group => <Link key={group.id} className={selectedSlug === group.slug ? styles.categorySelected : ""} href={href(group.slug)}>{group.name}</Link>)}</div>}
      </div>;
    })}
  </nav>;
}
