"use client";

import { useState, type FormEvent } from "react";
import styles from "./public-header.module.css";

const searchSuggestions = [
  "Aradığın Her Şey",
  "Bonna koleksiyonları",
  "Profesyonel mutfak ekipmanları",
  "Showroom ürünleri"
];

export function HeaderSearch({ locale = "tr" }: { locale?: "tr" | "en" }) {
  const [query, setQuery] = useState("");
  const [focused, setFocused] = useState(false);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const term = query.trim();
    if (term) window.location.href = `${locale === "en" ? "/en/products" : "/katalog"}?q=${encodeURIComponent(term)}`;
  }

  return (
    <form className={`${styles.headerSearchForm}${focused ? ` ${styles.headerSearchFormActive}` : ""}`} onSubmit={submit} role="search">
      <span className={styles.searchSuggestion} aria-hidden="true">{!query && !focused ? (locale === "en" ? "Everything you need" : searchSuggestions[0]) : ""}</span>
      <input
        value={query}
        onChange={(event) => setQuery(event.target.value)}
        onFocus={() => setFocused(true)}
        onBlur={() => setFocused(false)}
        placeholder=""
        aria-label={locale === "en" ? "Search products" : "Ürün ara"}
      />
      {query && <button className={styles.searchClear} type="button" onClick={() => setQuery("")} aria-label={locale === "en" ? "Clear search" : "Aramayı temizle"}>×</button>}
      <button type="submit" aria-label={locale === "en" ? "Search" : "Ara"}><span aria-hidden="true" /></button>
    </form>
  );
}
