import { getCatalogNavigation } from "../lib/catalog-api";
import { ProductsCategoryBrowser } from "./products-category-browser";

export async function ProductsMegaMenu({ locale = "tr" }: { locale?: "tr" | "en" }) {
  const categories = await getCatalogNavigation(locale).then(value => value.categories).catch(() => []);
  return <ProductsCategoryBrowser categories={categories} locale={locale} />;
}
