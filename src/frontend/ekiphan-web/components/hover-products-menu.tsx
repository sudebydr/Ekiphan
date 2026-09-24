"use client";

import type { FocusEvent, ReactNode } from "react";
import { useState } from "react";

type HoverProductsMenuProps = {
  children: ReactNode;
  className: string;
};

export function HoverProductsMenu({ children, className }: HoverProductsMenuProps) {
  const [isOpen, setIsOpen] = useState(false);

  function handleBlur(event: FocusEvent<HTMLDetailsElement>) {
    if (!event.currentTarget.contains(event.relatedTarget)) {
      setIsOpen(false);
    }
  }

  return (
    <details
      className={className}
      open={isOpen}
      onBlur={handleBlur}
      onFocus={() => setIsOpen(true)}
      onMouseEnter={() => setIsOpen(true)}
      onMouseLeave={() => setIsOpen(false)}
      onToggle={(event) => setIsOpen(event.currentTarget.open)}
    >
      {children}
    </details>
  );
}
