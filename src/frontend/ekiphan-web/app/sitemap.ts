import type { MetadataRoute } from "next";
import {
  getBrands,
  getCatalogSitemap,
  getContentSitemap
} from "../lib/catalog-api";
import { getSiteOrigin } from "../lib/site-url";

export const dynamic = "force-dynamic";

const corporateRoutes = new Set([
  "hakkimizda",
  "hizmetler",
  "referanslar",
  "galeri",
  "basin-odasi",
  "iletisim"
]);

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const origin = getSiteOrigin();
  if (!origin) return [];

  const entries: MetadataRoute.Sitemap = [
    {
      url: new URL("/", origin).href,
      changeFrequency: "weekly",
      priority: 1
    },
    {
      url: new URL("/katalog", origin).href,
      changeFrequency: "daily",
      priority: 0.9
    },
    {
      url: new URL("/showroom", origin).href,
      changeFrequency: "monthly",
      priority: 0.7
    },
    ...["/en", "/en/products", "/en/services", "/en/about", "/en/industrial-kitchen", "/en/references", "/en/gallery", "/en/catalogs", "/en/contact", "/en/press", "/en/showroom"].map((path) => ({
      url: new URL(path, origin).href,
      changeFrequency: "monthly" as const,
      priority: path === "/en" ? 1 : 0.6
    }))
  ];

  try {
    const products = await getCatalogSitemap();
    entries.push(
      ...products.map((product) => ({
        url: new URL(
          product.kind === "category"
            ? `/katalog?category=${encodeURIComponent(product.slug)}`
            : `/katalog/${encodeURIComponent(product.slug)}`,
          origin
        ).href,
        lastModified: product.updatedAt,
        changeFrequency: "weekly" as const,
        priority: 0.8
      }))
    );
  } catch {
    // Static public routes remain discoverable while the catalog is unavailable.
  }

  try {
    const contentPages = await getContentSitemap();
    entries.push(
      ...contentPages.map((page) => ({
        url: new URL(
          corporateRoutes.has(page.slug)
            ? `/${encodeURIComponent(page.slug)}`
            : `/sayfa/${encodeURIComponent(page.slug)}`,
          origin
        ).href,
        lastModified: page.updatedAt,
        changeFrequency: "monthly" as const,
        priority: 0.6
      }))
    );
  } catch {
    // Catalog routes remain discoverable while managed content is unavailable.
  }

  return entries;
}
