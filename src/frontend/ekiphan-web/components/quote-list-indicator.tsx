"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import {
  quoteListChangedEvent,
  quoteStorageKey,
  readQuoteList
} from "../lib/quote-list";

export function QuoteListIndicator({ className }: { className?: string }) {
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
      href="/teklif-listem"
      aria-current={pathname === "/teklif-listem" ? "page" : undefined}
      aria-label={`Teklif listem, ${count} ürün`}
    >
      Teklif listem{count > 0 ? ` (${count})` : ""}
    </Link>
  );
}
