"use client";

import type { FocusEvent, PointerEvent, ReactNode } from "react";
import { useEffect, useRef, useState } from "react";

type HoverProductsMenuProps = {
  children: ReactNode;
  className: string;
};

export function HoverProductsMenu({ children, className }: HoverProductsMenuProps) {
  const [isOpen, setIsOpen] = useState(false);
  const ref = useRef<HTMLDetailsElement>(null);
  useEffect(() => {
    const outside = (event: globalThis.PointerEvent) => {
      if (!ref.current?.contains(event.target as Node)) setIsOpen(false);
    };
    document.addEventListener("pointerdown", outside);
    return () => document.removeEventListener("pointerdown", outside);
  }, []);

  function handleBlur(event: FocusEvent<HTMLDetailsElement>) {
    if (!event.currentTarget.contains(event.relatedTarget)) {
      setIsOpen(false);
    }
  }

  function handlePointerEnter(event: PointerEvent<HTMLDetailsElement>) {
    if (event.pointerType === "mouse") setIsOpen(true);
  }

  function handlePointerLeave(event: PointerEvent<HTMLDetailsElement>) {
    if (event.pointerType === "mouse") setIsOpen(false);
  }

  return (
    <details
      ref={ref}
      onKeyDown={event => { if (event.key === "Escape") { setIsOpen(false); ref.current?.querySelector("summary")?.focus(); } }}
      onClick={event => { if ((event.target as HTMLElement).closest("a")) setIsOpen(false); }}
      className={className}
      open={isOpen}
      onBlur={handleBlur}
      onPointerEnter={handlePointerEnter}
      onPointerLeave={handlePointerLeave}
      onToggle={(event) => setIsOpen(event.currentTarget.open)}
    >
      {children}
    </details>
  );
}
