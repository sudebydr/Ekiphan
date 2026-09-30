"use client";

import { useState, type FormEvent } from "react";
import styles from "./public-header.module.css";

const searchSuggestions = [
  "Aradığın Her Şey",
  "Bonna koleksiyonları",
  "Profesyonel mutfak ekipmanları",
  "Showroom ürünleri"
];

export function HeaderSearch() {
  const [query, setQuery] = useState("");
  const [focused, setFocused] = useState(false);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const term = query.trim();
    if (term) window.location.href = `/katalog?q=${encodeURIComponent(term)}`;
  }

  return (
    <form className={`${styles.headerSearchForm}${focused ? ` ${styles.headerSearchFormActive}` : ""}`} onSubmit={submit} role="search">
      <span className={styles.searchSuggestion} aria-hidden="true">{!query && !focused ? searchSuggestions[0] : ""}</span>
      <input
        value={query}
        onChange={(event) => setQuery(event.target.value)}
        onFocus={() => setFocused(true)}
        onBlur={() => setFocused(false)}
        placeholder=""
        aria-label="Ürün ara"
      />
      {query && <button className={styles.searchClear} type="button" onClick={() => setQuery("")} aria-label="Aramayı temizle">×</button>}
      <button type="submit" aria-label="Ara"><span aria-hidden="true" /></button>
    </form>
  );
}
