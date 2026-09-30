"use client";

import { useEffect } from "react";

export function ScrollNavbar() {
  useEffect(() => {
    const header = document.querySelector<HTMLElement>(
      'header[data-sticky-nav="true"]'
    );

    if (!header) return;

    const nav = header.querySelector<HTMLElement>(
      'nav[aria-label="Ana menü"]'
    );

    if (!nav) return;

    const spacer = document.createElement("div");
    spacer.setAttribute("aria-hidden", "true");
    spacer.style.display = "none";
    nav.before(spacer);

    const setSticky = (sticky: boolean) => {
      if (sticky) {
        spacer.style.height = `${nav.offsetHeight}px`;
        spacer.style.display = "block";
        nav.classList.add("ekiphan-sticky-nav-fixed");
      } else {
        nav.classList.remove("ekiphan-sticky-nav-fixed");
        spacer.style.display = "none";
        spacer.style.height = "0px";
      }
    };

    const handleScroll = () => {
      if (header.classList.contains("ekiphan-mobile-open")) {
        setSticky(false);
        return;
      }

      if (nav.offsetHeight === 0) {
        setSticky(false);
        return;
      }

      const navTop =
        (spacer.style.display === "block"
          ? spacer.getBoundingClientRect().top
          : nav.getBoundingClientRect().top) + window.scrollY;

      setSticky(window.scrollY >= navTop);
    };

    const handleResize = () => {
      handleScroll();
    };

    const menuObserver = new MutationObserver(handleScroll);
    menuObserver.observe(header, {
      attributes: true,
      attributeFilter: ["class"]
    });

    handleScroll();

    window.addEventListener(
      "scroll",
      handleScroll,
      { passive: true }
    );
    window.addEventListener("resize", handleResize);

    return () => {
      window.removeEventListener(
        "scroll",
        handleScroll
      );
      window.removeEventListener("resize", handleResize);
      menuObserver.disconnect();
      nav.classList.remove("ekiphan-sticky-nav-fixed");
      spacer.remove();
    };
  }, []);

  return null;
}
