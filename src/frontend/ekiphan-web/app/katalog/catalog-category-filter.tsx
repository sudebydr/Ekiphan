"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import type { CatalogNavigation } from "../../lib/catalog-types";
import { optionHref } from "./catalog-filter-links";
import styles from "./catalog-list.module.css";

type Category = CatalogNavigation["categories"][number];

export function CatalogCategoryFilter({
  categories,
  selectedSlug,
  baseQuery,
  isEnglish = false
}: {
  categories: Category[];
  selectedSlug: string;
  baseQuery: string;
  isEnglish?: boolean;
}) {
  const roots = categories.filter((item) => item.parentId === null);
  const childrenByParent = new Map<string, Category[]>();
  for (const item of categories) {
    if (item.parentId) {
      childrenByParent.set(item.parentId, [...(childrenByParent.get(item.parentId) ?? []), item]);
    }
  }

  const selectedRootId = roots.find(
    (root) =>
      root.slug === selectedSlug ||
      (childrenByParent.get(root.id) ?? []).some((child) => child.slug === selectedSlug)
  )?.id;
  const selectedName = categories.find((item) => item.slug === selectedSlug)?.name;

  const [open, setOpen] = useState<Set<string>>(
    () => new Set(selectedRootId ? [selectedRootId] : [])
  );

  // Bir kategoriye tıklanınca alt kategorileri otomatik açılsın.
  useEffect(() => {
    if (!selectedRootId) return;
    setOpen((current) => (current.has(selectedRootId) ? current : new Set(current).add(selectedRootId)));
  }, [selectedRootId]);

  const optionClass = (selected: boolean) =>
    `${styles.option} ${selected ? styles.optionSelected : ""}`;

  return (
    <details className={styles.filterGroup} open={Boolean(selectedSlug)}>
      <summary>
        <span>{isEnglish ? "Categories" : "Kategoriler"}</span>
        {selectedName && <span className={styles.filterCurrent}>{selectedName}</span>}
      </summary>
      <ul className={styles.optionList}>
        <li>
          <Link
            className={optionClass(!selectedSlug)}
            href={optionHref(baseQuery, "category", "")}
            aria-current={!selectedSlug ? "true" : undefined}
            scroll={false}
            prefetch={false}
          >
            {isEnglish ? "All categories" : "Tüm kategoriler"}
          </Link>
        </li>
        {roots.map((root) => {
          const groups = childrenByParent.get(root.id) ?? [];
          const expanded = open.has(root.id);
          return (
            <li key={root.id}>
              <div className={styles.categoryRow}>
                <Link
                  className={optionClass(selectedSlug === root.slug)}
                  href={optionHref(baseQuery, "category", root.slug)}
                  aria-current={selectedSlug === root.slug ? "true" : undefined}
                  scroll={false}
                  prefetch={false}
                >
                  {root.name}
                </Link>
                {groups.length > 0 && (
                  <button
                    type="button"
                    className={styles.categoryToggle}
                    aria-expanded={expanded}
                    aria-controls={`category-${root.id}`}
                    aria-label={`${root.name} alt kategorileri`}
                    onClick={() =>
                      setOpen((current) => {
                        const next = new Set(current);
                        if (expanded) next.delete(root.id);
                        else next.add(root.id);
                        return next;
                      })
                    }
                  >
                    <span className={styles.chevron} aria-hidden="true" />
                  </button>
                )}
              </div>
              {groups.length > 0 && expanded && (
                <ul id={`category-${root.id}`} className={styles.subList}>
                  {groups.map((group) => (
                    <li key={group.id}>
                      <Link
                        className={optionClass(selectedSlug === group.slug)}
                        href={optionHref(baseQuery, "category", group.slug)}
                        aria-current={selectedSlug === group.slug ? "true" : undefined}
                        scroll={false}
                        prefetch={false}
                      >
                        {group.name}
                      </Link>
                    </li>
                  ))}
                </ul>
              )}
            </li>
          );
        })}
      </ul>
    </details>
  );
}
