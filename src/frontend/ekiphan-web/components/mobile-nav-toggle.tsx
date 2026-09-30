"use client";

import { useEffect, useRef, useState } from "react";
import { usePathname } from "next/navigation";
import styles from "./public-header.module.css";

export default function MobileNavToggle() {
  const buttonRef = useRef<HTMLButtonElement>(null);
  const [isOpen, setIsOpen] = useState(false);
  const pathname = usePathname();

  useEffect(() => {
    setIsOpen(false);
  }, [pathname]);

  useEffect(() => {
    const header = buttonRef.current?.closest("header");
    if (!header) return;

    const previousOverflow = document.body.style.overflow;
    header.classList.toggle("ekiphan-mobile-open", isOpen);
    document.body.style.overflow = isOpen ? "hidden" : previousOverflow;

    const closeOnLink = (event: Event) => {
      const target = event.target;
      if (target instanceof Element && target.closest('nav[aria-label="Ana menü"] a')) {
        setIsOpen(false);
      }
    };
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") setIsOpen(false);
    };

    header.addEventListener("click", closeOnLink);
    document.addEventListener("keydown", closeOnEscape);

    return () => {
      header.classList.remove("ekiphan-mobile-open");
      document.body.style.overflow = previousOverflow;
      header.removeEventListener("click", closeOnLink);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, [isOpen]);

  return (
    <button
      ref={buttonRef}
      className={styles.mobileMenuButton}
      type="button"
      aria-label={isOpen ? "Menüyü kapat" : "Menüyü aç"}
      aria-controls="public-mobile-navigation"
      aria-expanded={isOpen}
      onClick={() => setIsOpen((open) => !open)}
    >
      <span />
      <span />
      <span />
    </button>
  );
}
