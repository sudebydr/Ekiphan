import type { Metadata } from "next";
import type { PublicContentPage } from "./content-types";

export function managedMetadata(page: PublicContentPage, fallbackPath: string): Metadata {
  const canonical = page.canonicalUrl ?? fallbackPath;
  const description = page.metaDescription ?? page.summary ?? undefined;
  return {
    title: page.metaTitle ?? page.title,
    description,
    alternates: {
      canonical,
      languages: Object.fromEntries((page.alternates ?? []).map((item) => [
        item.languageCode === "tr" ? "tr-TR" : "en",
        `/sayfa/${encodeURIComponent(item.slug)}?lang=${item.languageCode}`
      ]))
    },
    robots: { index: !page.noIndex, follow: !page.noFollow },
    openGraph: {
      type: "website",
      locale: page.languageCode === "en" ? "en_US" : "tr_TR",
      title: page.openGraphTitle ?? page.metaTitle ?? page.title,
      description: page.openGraphDescription ?? description,
      url: canonical,
      images: page.openGraphImageUrl
        ? [{ url: page.openGraphImageUrl, alt: page.title }]
        : undefined
    },
    twitter: {
      card: page.openGraphImageUrl ? "summary_large_image" : "summary",
      title: page.openGraphTitle ?? page.metaTitle ?? page.title,
      description: page.openGraphDescription ?? description,
      images: page.openGraphImageUrl ? [page.openGraphImageUrl] : undefined
    }
  };
}
