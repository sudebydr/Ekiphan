"use client";

import type { FormEvent, ReactNode } from "react";
import { useEffect, useRef, useTransition } from "react";
import { usePathname, useRouter } from "next/navigation";

type CatalogFilterFormProps = {
  children: ReactNode;
  className: string;
};

/**
 * Yalnızca metin araması ve sayısal aralık filtrelerini gönderir.
 * Seçenek listeleri (grup, kategori, marka, etiket, sıralama) bağlantı olduğundan
 * mevcut seçimler formda gizli alanlarla taşınır.
 */
export function CatalogFilterForm({ children, className }: CatalogFilterFormProps) {
  const router = useRouter();
  const pathname = usePathname();
  const formRef = useRef<HTMLFormElement>(null);
  useEffect(() => {
    const panel = formRef.current?.querySelector<HTMLDetailsElement>("details");
    if (panel) panel.open = !window.matchMedia("(max-width: 58rem)").matches;
  }, []);
  const [isPending, startTransition] = useTransition();

  const applyFilters = (form: HTMLFormElement) => {
    const query = new URLSearchParams();
    for (const [key, rawValue] of new FormData(form).entries()) {
      const value = String(rawValue).trim();
      if (value) query.append(key, value);
    }
    query.delete("page");
    query.delete("pageSize");
    const suffix = query.toString();
    startTransition(() =>
      router.replace(suffix ? `${pathname}?${suffix}` : pathname, { scroll: false })
    );
  };

  return (
    <form
      ref={formRef}
      className={className}
      onSubmit={(event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        applyFilters(event.currentTarget);
      }}
      aria-busy={isPending}
    >
      {children}
    </form>
  );
}
