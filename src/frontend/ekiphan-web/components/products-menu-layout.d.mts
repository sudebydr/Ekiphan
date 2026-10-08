import type { CatalogNavigation } from "../lib/catalog-types";
export function menuBounds(viewportWidth: number, viewportHeight: number, triggerLeft: number, triggerBottom: number): { width: number; left: number; top: number; maxHeight: number };
export function categoryPath(categories: CatalogNavigation["categories"], id: string | null): CatalogNavigation["categories"];
export function createHoverIntent(select: (id: string) => void, delay?: number): { cancel(): void; schedule(id: string): void };
