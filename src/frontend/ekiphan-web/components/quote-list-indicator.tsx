"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import {
  quoteListChangedEvent,
  quoteStorageKey,
  readQuoteList
} from "../lib/quote-list";

export function QuoteListIndicator({ className, locale = "tr" }: { className?: string; locale?: "tr" | "en" }) {
  const [count, setCount] = useState(0);
  const pathname = usePathname();

  useEffect(() => {
    const update = () => setCount(readQuoteList().length);
    const storage = (event: StorageEvent) => {
      if (event.key === quoteStorageKey) update();
    };
    update();
    window.addEventListener(quoteListChangedEvent, update);
    window.addEventListener("storage", storage);
    return () => {
      window.removeEventListener(quoteListChangedEvent, update);
      window.removeEventListener("storage", storage);
    };
  }, []);

  return (
    <Link
      className={className}
      href={locale === "en" ? "/en/quote-list" : "/teklif-listem"}
      aria-current={pathname === (locale === "en" ? "/en/quote-list" : "/teklif-listem") ? "page" : undefined}
      aria-label={locale === "en" ? `My quote list, ${count} items` : `Teklif listem, ${count} ürün`}
    >
      {locale === "en" ? "My quote list" : "Teklif listem"}{count > 0 ? ` (${count})` : ""}
    </Link>
  );
}
