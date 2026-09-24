"use client";

import { useEffect, useState, type FormEvent } from "react";
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
  const [suggestionIndex, setSuggestionIndex] = useState(0);
  const [visibleSuggestion, setVisibleSuggestion] = useState("");
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    if (focused || query) return;

    const target = searchSuggestions[suggestionIndex];
    const isComplete = visibleSuggestion === target;
    const isEmpty = visibleSuggestion.length === 0;
    const delay = isComplete && !isDeleting ? 1450 : isEmpty && isDeleting ? 350 : isDeleting ? 38 : 76;

    const timeout = window.setTimeout(() => {
      if (isComplete && !isDeleting) {
        setIsDeleting(true);
        return;
      }

      if (isEmpty && isDeleting) {
        setIsDeleting(false);
        setSuggestionIndex((index) => (index + 1) % searchSuggestions.length);
        return;
      }

      setVisibleSuggestion((current) =>
        isDeleting ? target.slice(0, Math.max(0, current.length - 1)) : target.slice(0, current.length + 1)
      );
    }, delay);

    return () => window.clearTimeout(timeout);
  }, [focused, query, suggestionIndex, visibleSuggestion, isDeleting]);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const term = query.trim();
    if (term) window.location.href = `/katalog?q=${encodeURIComponent(term)}`;
  }

  return (
    <form className={`${styles.headerSearchForm}${focused ? ` ${styles.headerSearchFormActive}` : ""}`} onSubmit={submit} role="search">
      <span className={styles.searchSuggestion} aria-hidden="true">{!query && !focused ? visibleSuggestion : ""}</span>
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