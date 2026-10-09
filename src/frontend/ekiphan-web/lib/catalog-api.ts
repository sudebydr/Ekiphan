import { cache } from "react";
import type {
  CatalogNavigation,
  CatalogBrandDetail,
  CatalogBrandListItem,
  CatalogFacet,
  CatalogPagedResult,
  CatalogProductDetail,
  CatalogSitemapEntry
} from "./catalog-types";
import type {
  PublicContentPage,
  PublicContentSitemapEntry
} from "./content-types";
import type { PublicMenuItem } from "./menu-types";
import type { PublicHomepageHero } from "./homepage-hero-types";
import type { PublicGalleryItem } from "./gallery-types";
import type { PublicPressRelease } from "./press-release-types";
import { getDemoCatalogNavigation, getDemoProduct, getDemoProducts, isDemoProductsEnabled } from "./demo-products";

export class CatalogApiError extends Error {
  constructor(
    message: string,
    public readonly status?: number
  ) {
    super(message);
  }
}

function apiBaseUrl(): string {
  const value =
    process.env.EKIPHAN_API_BASE_URL ??
    process.env.NEXT_PUBLIC_API_BASE_URL;

  if (!value) {
    throw new CatalogApiError("Katalog servisi henüz yapılandırılmamış.");
  }

  return value.replace(/\/+$/, "");
}

async function getJson<T>(path: string): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl()}${path}`, {
      cache: "no-store",
      headers: { Accept: "application/json" },
      signal: AbortSignal.timeout(8_000)
    });
  } catch {
    throw new CatalogApiError(
      "Katalog servisine şu anda ulaşılamıyor. Lütfen daha sonra tekrar deneyin."
    );
  }

  if (response.status === 404) {
    throw new CatalogApiError("Ürün bulunamadı.", 404);
  }

  if (!response.ok) {
    throw new CatalogApiError(
      "Katalog bilgileri şu anda görüntülenemiyor.",
      response.status
    );
  }

  const api = new URL(apiBaseUrl());
  if (["localhost", "127.0.0.1", "[::1]"].includes(api.hostname)) {
    return JSON.parse(await response.text(), (_key, value: unknown) => {
      if (typeof value !== "string" || !value.startsWith(`${api.origin}/media/`)) return value;
      return value.slice(api.origin.length);
    }) as T;
  }
  return (await response.json()) as T;
}

export const getCatalogNavigation = cache(async (language: "tr" | "en" = "tr"): Promise<CatalogNavigation> => {
  if (isDemoProductsEnabled()) return getDemoCatalogNavigation();
  try { return await getJson(`/api/catalog/${language}/navigation`); }
  catch (error) { if (language === "en") return getJson("/api/catalog/tr/navigation"); throw error; }
});
export async function getCatalogFacets(category: string, language: "tr" | "en" = "tr"): Promise<CatalogFacet[]> {
  if (isDemoProductsEnabled()) return [];

  try {
    return await getJson(`/api/catalog/${language}/facets?category=${encodeURIComponent(category)}`);
  } catch {
    if (language === "en") {
      try { return await getJson(`/api/catalog/tr/facets?category=${encodeURIComponent(category)}`); } catch { /* Keep empty facets available as a fallback. */ }
    }
    return [];
  }
}
export function getCatalogSitemap(): Promise<CatalogSitemapEntry[]> {
  return getJson("/api/catalog/tr/sitemap");
}

export async function getBrands(language: "tr" | "en" = "tr"): Promise<CatalogBrandListItem[]> {
  try { return await getJson(`/api/catalog/${language}/brands`); }
  catch (error) { if (language === "en") return getJson("/api/catalog/tr/brands"); throw error; }
}

export type CatalogPdfDocument = { id: string; slug: string; title: string; fileName: string; url: string; coverUrl: string; createdAt: string };

export async function getCatalogPdfDocuments(): Promise<CatalogPdfDocument[]> {
  try {
    const documents = await getJson<CatalogPdfDocument[]>("/api/catalogs/documents");
    const localApi = ["localhost", "127.0.0.1", "[::1]"].includes(new URL(apiBaseUrl()).hostname);
    return documents.map((document) => ({
      ...document,
      url: document.url,
      // Local /media rewrites keep covers same-origin and allowed by img-src 'self'.
      coverUrl: !localApi && document.coverUrl.startsWith("/media/") ? `${apiBaseUrl()}${document.coverUrl}` : document.coverUrl
    }));
  } catch {
    return [];
  }
}

export function getBrand(slug: string): Promise<CatalogBrandDetail> {
  return getJson(`/api/catalog/tr/brands/${encodeURIComponent(slug)}`);
}

export async function getProducts(
  query: URLSearchParams,
  language: "tr" | "en" = "tr"
): Promise<CatalogPagedResult> {
  if (isDemoProductsEnabled()) return getDemoProducts(query);

  const queryString = query.toString();
  try { return await getJson(`/api/catalog/${language}/products${queryString ? `?${queryString}` : ""}`); }
  catch (error) { if (language === "en") return getJson(`/api/catalog/tr/products${queryString ? `?${queryString}` : ""}`); throw error; }
}
export async function getProduct(slug: string, language: "tr" | "en" = "tr"): Promise<CatalogProductDetail> {
  if (isDemoProductsEnabled()) {
    const product = getDemoProduct(slug);
    if (!product) throw new CatalogApiError("Ürün bulunamadı.", 404);
    return product;
  }

  return getJson(`/api/catalog/${language}/products/${encodeURIComponent(slug)}`);
}
export function getContentPage(slug: string, language: "tr" | "en" = "tr"): Promise<PublicContentPage> {
  return getJson(`/api/content/${language}/pages/${encodeURIComponent(slug)}`);
}

export function getContentSitemap(): Promise<PublicContentSitemapEntry[]> {
  return getJson("/api/content/tr/sitemap");
}

export function getPublicMenu(
  location: "Header" | "Footer"
): Promise<PublicMenuItem[]> {
  return getJson(`/api/navigation/tr/${location}`);
}

export function getHomepageHeroes(): Promise<PublicHomepageHero[]> {
  return getJson("/api/home/tr/heroes");
}

export function getGallery(): Promise<PublicGalleryItem[]> {
  return getJson("/api/gallery/tr");
}

export function getPressReleases(language: "tr" | "en" = "tr"): Promise<PublicPressRelease[]> {
  return getJson(`/api/press/${language}`);
}
