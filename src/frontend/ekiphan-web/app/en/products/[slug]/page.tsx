import ProductDetailPage from "../../../katalog/product-detail-page";
import { generateMetadata as productMetadata } from "../../../katalog/product-detail-page";
import type { Metadata } from "next";

type Params = { slug: string };

export async function generateMetadata({ params }: { params: Promise<Params> }): Promise<Metadata> {
  return productMetadata({ params, searchParams: Promise.resolve({ lang: "en" }) });
}

export default async function EnglishProductDetailPage({ params }: { params: Promise<Params> }) {
  const resolved = await params;
  return ProductDetailPage({ params: Promise.resolve(resolved), searchParams: Promise.resolve({ lang: "en" }) });
}
