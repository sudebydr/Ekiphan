"use client";

import { useState } from "react";
import type { CatalogFacet } from "../../lib/catalog-types";
import { FilterOptionGroup } from "./catalog-filter-group";
import { attributeOptionHref } from "./catalog-filter-links";
import styles from "./catalog-list.module.css";

function selectedValue(selected: string[], attributeId: string): string {
  const prefix = `${attributeId}:`;
  return selected.find((value) => value.startsWith(prefix))?.slice(prefix.length) ?? "";
}

function NumericFacet({
  facet,
  initial
}: {
  facet: CatalogFacet;
  initial: string;
}) {
  const range = initial.startsWith("n:") ? initial.slice(2).split(":") : ["", ""];
  const [minimum, setMinimum] = useState(range[0] ?? "");
  const [maximum, setMaximum] = useState(range[1] ?? "");
  const value = minimum || maximum
    ? `${facet.attributeId}:n:${minimum}:${maximum}`
    : "";
  return (
    <fieldset className={styles.rangeField}>
      <legend>{facet.name}{facet.unitSymbol ? ` (${facet.unitSymbol})` : ""}</legend>
      <div>
        <label>En az<input type="number" step="any"
          min={facet.minimum ?? undefined} max={facet.maximum ?? undefined}
          placeholder={facet.minimum?.toLocaleString("tr-TR")}
          value={minimum} onChange={(event) => setMinimum(event.target.value)} /></label>
        <label>En çok<input type="number" step="any"
          min={facet.minimum ?? undefined} max={facet.maximum ?? undefined}
          placeholder={facet.maximum?.toLocaleString("tr-TR")}
          value={maximum} onChange={(event) => setMaximum(event.target.value)} /></label>
      </div>
      {value && <input type="hidden" name="attribute" value={value} />}
    </fieldset>
  );
}

export function DynamicCatalogFilters({
  facets,
  selected,
  baseQuery
}: {
  facets: CatalogFacet[];
  selected: string[];
  baseQuery: string;
}) {
  if (facets.length === 0) return null;
  return (
    <div className={styles.dynamicFilters}>
      {facets.map((facet) => {
        const current = selectedValue(selected, facet.attributeId);
        if (facet.dataType === 2) {
          return (
            <details className={styles.filterGroup} key={facet.attributeId} open={Boolean(current)}>
              <summary><span>{facet.name}</span></summary>
              <NumericFacet facet={facet} initial={current} />
            </details>
          );
        }
        return (
          <FilterOptionGroup
            key={facet.attributeId}
            title={facet.name}
            options={[
              {
                value: "",
                label: "Tümü",
                href: attributeOptionHref(baseQuery, facet.attributeId, ""),
                selected: !current
              },
              ...facet.options.map((option) => ({
                value: option.value,
                label: option.label,
                href: attributeOptionHref(baseQuery, facet.attributeId, option.value),
                selected: current === option.value
              }))
            ]}
          />
        );
      })}
    </div>
  );
}
