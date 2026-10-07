import type { Metadata } from "next";
import CatalogPage from "../../katalog/page";

type Params = Record<string, string | string[] | undefined>;

export const metadata: Metadata = { title: "Products | Ekiphan", description: "Browse Ekiphan professional kitchen equipment.", alternates: { canonical: "/en/products", languages: { tr: "/katalog", en: "/en/products" } } };

export default async function EnglishProductsPage({ searchParams }: { searchParams: Promise<Params> }) {
  const params = await searchParams;
  return CatalogPage({ searchParams: Promise.resolve({ ...params, lang: "en" }) });
}
