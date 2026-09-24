"use client";

import type { FormEvent, ReactNode } from "react";
import { useRef, useTransition } from "react";
import { useRouter } from "next/navigation";

type CatalogFilterFormProps = {
  children: ReactNode;
  className: string;
};

export function CatalogFilterForm({ children, className }: CatalogFilterFormProps) {
  const router = useRouter();
  const formRef = useRef<HTMLFormElement>(null);
  const [isPending, startTransition] = useTransition();

  const applyFilters = (form: HTMLFormElement) => {
    const formData = new FormData(form);
    const query = new URLSearchParams();
    for (const [key, rawValue] of formData.entries()) {
      const value = String(rawValue).trim();
      if (value) query.append(key, value);
    }
    query.delete("page");
    query.delete("pageSize");
    const suffix = query.toString();
    startTransition(() =>
      router.replace(suffix ? `/katalog?${suffix}` : "/katalog", { scroll: false })
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
      onChange={(event) => {
        const target = event.target;
        if (
          target instanceof HTMLSelectElement &&
          target.dataset.quickFilter === "true"
        ) {
          applyFilters(event.currentTarget);
        }
      }}
      onReset={(event) => {
        event.preventDefault();
        event.currentTarget.reset();
        startTransition(() => router.replace("/katalog", { scroll: false }));
      }}
      aria-busy={isPending}
    >
      {children}
    </form>
  );
}

