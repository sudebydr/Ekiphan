"use client";

import type { FocusEvent, PointerEvent, ReactNode } from "react";
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

  function handlePointerEnter(event: PointerEvent<HTMLDetailsElement>) {
    if (event.pointerType === "mouse") setIsOpen(true);
  }

  function handlePointerLeave(event: PointerEvent<HTMLDetailsElement>) {
    if (event.pointerType === "mouse") setIsOpen(false);
  }

  return (
    <details
      className={className}
      open={isOpen}
      onBlur={handleBlur}
      onFocus={() => setIsOpen(true)}
      onPointerEnter={handlePointerEnter}
      onPointerLeave={handlePointerLeave}
      onToggle={(event) => setIsOpen(event.currentTarget.open)}
    >
      {children}
    </details>
  );
}
