"use client";

import { useState } from "react";
import type { CatalogFacet } from "../../lib/catalog-types";
import styles from "./catalog.module.css";

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
  selected
}: {
  facets: CatalogFacet[];
  selected: string[];
}) {
  if (facets.length === 0) return null;
  return (
    <div className={styles.dynamicFilters}>
      <h3>Teknik özellikler</h3>
      {facets.map((facet) => {
        const current = selectedValue(selected, facet.attributeId);
        if (facet.dataType === 2) {
          return <NumericFacet key={facet.attributeId} facet={facet} initial={current} />;
        }
        return (
          <div className={styles.field} key={facet.attributeId}>
            <label htmlFor={`facet-${facet.attributeId}`}>{facet.name}</label>
            <select id={`facet-${facet.attributeId}`} name="attribute"
              defaultValue={current ? `${facet.attributeId}:${current}` : ""}>
              <option value="">Tümü</option>
              {facet.options.map((option) => (
                <option value={`${facet.attributeId}:${option.value}`} key={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
        );
      })}
    </div>
  );
}
