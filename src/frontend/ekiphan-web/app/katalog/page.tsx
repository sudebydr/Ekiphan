import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../../components/public-header";
import { QuoteListIndicator } from "../../components/quote-list-indicator";
import {
  CatalogApiError,
  getCatalogFacets,
  getCatalogNavigation,
  getProducts
} from "../../lib/catalog-api";
import type {
  CatalogFacet,
  CatalogNavigation,
  CatalogPagedResult
} from "../../lib/catalog-types";
import { defaultSocialImage } from "../../lib/social-metadata";
import { DynamicCatalogFilters } from "./dynamic-catalog-filters";
import { CatalogFilterForm } from "./catalog-filter-form";
import { CatalogCategoryFilter } from "./catalog-category-filter";
import { FilterOptionGroup, type FilterOption } from "./catalog-filter-group";
import { optionHref } from "./catalog-filter-links";
import { CatalogProductCard } from "./catalog-product-card";
import styles from "./catalog-list.module.css";

export const dynamic = "force-dynamic";

type SearchParams = Record<string, string | string[] | undefined>;

function one(value: string | string[] | undefined): string {
  return Array.isArray(value) ? (value[0] ?? "") : (value ?? "");
}

function many(value: string | string[] | undefined): string[] {
  return (Array.isArray(value) ? value : value ? [value] : [])
    .map((item) => item.trim()).filter(Boolean).slice(0, 20);
}

export async function generateMetadata({ searchParams }: { searchParams: Promise<SearchParams> }): Promise<Metadata> {
  const params = await searchParams;
  const isEnglish = one(params.lang) === "en";
  const categorySlug = one(params.category).trim();
  if (categorySlug) {
    try {
      const navigation = await getCatalogNavigation(isEnglish ? "en" : "tr");
      const category = navigation.categories.find((item) => item.slug === categorySlug);
      if (category) {
        const canonical = category.canonicalUrl ?? `/katalog?category=${encodeURIComponent(category.slug)}`;
        const description = category.metaDescription ?? `${category.name} ürünleri ve profesyonel mutfak çözümleri.`;
        return { title: category.metaTitle ?? category.name, description,
          alternates: { canonical }, robots: { index: !category.noIndex, follow: !category.noFollow },
          openGraph: { type: "website", locale: "tr_TR", title: category.openGraphTitle ?? category.metaTitle ?? category.name, description: category.openGraphDescription ?? description, url: canonical, images: category.openGraphImageUrl ? [category.openGraphImageUrl] : defaultSocialImage ? [defaultSocialImage] : undefined } };
      }
    } catch { /* Fall back to catalog metadata. */ }
  }
  return { title: isEnglish ? "Product Catalogue | Ekiphan" : "Ürün Kataloğu", description: isEnglish ? "Browse Ekiphan's professional hotel, restaurant and industrial kitchen equipment." : "Ekiphan otel, restoran ve endüstriyel mutfak ekipmanları kataloğu.", alternates: { canonical: isEnglish ? "/en/products" : "/katalog", languages: { tr: "/katalog", en: "/en/products" } }, openGraph: { type: "website", locale: isEnglish ? "en_US" : "tr_TR", title: isEnglish ? "Product Catalogue | Ekiphan" : "Ürün Kataloğu", description: isEnglish ? "Browse Ekiphan's professional hotel, restaurant and industrial kitchen equipment." : "Ekiphan otel, restoran ve endüstriyel mutfak ekipmanları kataloğu.", images: defaultSocialImage ? [defaultSocialImage] : undefined, url: isEnglish ? "/en/products" : "/katalog" } };
}

function productQuery(params: SearchParams): URLSearchParams {
  const query = new URLSearchParams();
  for (const key of [
    "q",
    "section",
    "category",
    "brand",
    "tag",
    "sort",
    "page"
  ]) {
    const value = one(params[key]).trim();
    if (value) query.set(key, value);
  }
  for (const value of many(params.attribute)) query.append("attribute", value);
  query.set("pageSize", "24");
  return query;
}

function pageHref(query: URLSearchParams, page: number, basePath = "/katalog"): string {
  const next = new URLSearchParams(query);
  next.set("page", String(page));
  return `${basePath}?${next.toString()}`;
}

function filterHref(query: URLSearchParams, key: string, value: string | undefined, basePath = "/katalog"): string {
  const next = new URLSearchParams(query);
  if (value === undefined) next.delete(key);
  else {
    const remaining = next.getAll(key).filter((item) => item !== value);
    next.delete(key);
    for (const item of remaining) next.append(key, item);
  }
  next.delete("page");
  next.delete("pageSize");
  const queryString = next.toString();
  return queryString ? `${basePath}?${queryString}` : basePath;
}

/** Filtre bağlantılarının temeli: şu anki tüm seçimler (sayfa hariç). */
function baseFilterQuery(params: SearchParams, isEnglish = false): string {
  const query = new URLSearchParams();
  for (const key of ["q", "section", "category", "brand", "tag", "sort"]) {
    const value = one(params[key]).trim();
    if (value) query.set(key, value);
  }
  for (const value of many(params.attribute)) query.append("attribute", value);
  if (isEnglish) query.set("lang", "en");
  if (one(params.view) === "list") query.set("view", "list");
  return query.toString();
}

function buildOptions(
  baseQuery: string,
  key: "section" | "brand" | "tag",
  allLabel: string,
  items: { slug: string | null; name: string }[],
  selected: string
): FilterOption[] {
  return [
    { value: "", label: allLabel, href: optionHref(baseQuery, key, ""), selected: !selected },
    ...items
      .filter((item): item is { slug: string; name: string } => Boolean(item.slug))
      .map((item) => ({
        value: item.slug,
        label: item.name,
        href: optionHref(baseQuery, key, item.slug),
        selected: selected === item.slug
      }))
  ];
}

type CatalogView = "grid" | "list";

function viewHref(params: SearchParams, view: CatalogView, basePath = "/katalog"): string {
  const next = new URLSearchParams();
  for (const [key, raw] of Object.entries(params)) {
    if (key === "view" || key === "page") continue;
    for (const value of Array.isArray(raw) ? raw : raw ? [raw] : []) {
      if (value) next.append(key, value);
    }
  }
  if (view === "list") next.set("view", "list");
  const queryString = next.toString();
  return queryString ? `${basePath}?${queryString}` : basePath;
}

function GridIcon() {
  return (
    <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
      <rect x="2" y="2" width="7" height="7" rx="1.2" />
      <rect x="11" y="2" width="7" height="7" rx="1.2" />
      <rect x="2" y="11" width="7" height="7" rx="1.2" />
      <rect x="11" y="11" width="7" height="7" rx="1.2" />
    </svg>
  );
}

function ListIcon() {
  return (
    <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
      <rect x="2" y="3" width="16" height="3.4" rx="1.2" />
      <rect x="2" y="8.3" width="16" height="3.4" rx="1.2" />
      <rect x="2" y="13.6" width="16" height="3.4" rx="1.2" />
    </svg>
  );
}

export default async function CatalogPage({
  searchParams
}: {
  searchParams: Promise<SearchParams>;
}) {
  const params = await searchParams;
  const isEnglish = one(params.lang) === "en";
  const basePath = isEnglish ? "/en/products" : "/katalog";
  const query = productQuery(params);
  const view: CatalogView = one(params.view) === "list" ? "list" : "grid";
  // Sayfa/filtre bağlantılarında görünüm tercihi korunur; API sorgusuna eklenmez.
  const linkQuery = new URLSearchParams(query);
  if (view === "list") linkQuery.set("view", "list");
  let navigation: CatalogNavigation | null = null;
  let facets: CatalogFacet[] = [];
  let products: CatalogPagedResult | null = null;
  let error: string | null = null;

  try {
    [navigation, products] = await Promise.all([
      getCatalogNavigation(isEnglish ? "en" : "tr"),
      getProducts(query, isEnglish ? "en" : "tr")
    ]);
    const category = one(params.category).trim();
    if (category) facets = await getCatalogFacets(category, isEnglish ? "en" : "tr");
  } catch (caught) {
    error =
      caught instanceof CatalogApiError
        ? caught.message
        : "Katalog şu anda görüntülenemiyor.";
  }

  const pageCount = products
    ? Math.max(1, Math.ceil(products.totalCount / products.pageSize))
    : 1;
  const attributeFilters = many(params.attribute);
  const activeFilters = [
    one(params.q).trim()
      ? { key: "q", label: `${isEnglish ? "Search" : "Arama"}: ${one(params.q).trim()}` }
      : null,
    one(params.section).trim()
      ? {
          key: "section",
          label: `${isEnglish ? "Group" : "Grup"}: ${
            navigation?.sections.find(
              (item) => item.slug === one(params.section).trim()
            )?.name ?? one(params.section).trim()
          }`
        }
      : null,
    one(params.category).trim()
      ? {
          key: "category",
          label: `${isEnglish ? "Category" : "Kategori"}: ${
            navigation?.categories.find(
              (item) => item.slug === one(params.category).trim()
            )?.name ?? one(params.category).trim()
          }`
        }
      : null,
    one(params.brand).trim()
      ? {
          key: "brand",
          label: `${isEnglish ? "Brand" : "Marka"}: ${
            navigation?.brands.find(
              (item) => item.slug === one(params.brand).trim()
            )?.name ?? one(params.brand).trim()
          }`
        }
      : null,
    one(params.tag).trim()
      ? {
          key: "tag",
          label: `${isEnglish ? "Tag" : "Etiket"}: ${
            navigation?.tags.find(
              (item) => item.slug === one(params.tag).trim()
            )?.name ?? one(params.tag).trim()
          }`
        }
      : null,
    ...attributeFilters.map((value) => {
      const separator = value.indexOf(":");
      const attributeId = value.slice(0, separator);
      const token = value.slice(separator + 1);
      const facet = facets.find((item) => item.attributeId === attributeId);
      const option = facet?.options.find((item) => item.value === token);
      const range = token.startsWith("n:") ? token.slice(2).split(":") : null;
      const shownValue = option?.label ?? (range
        ? `${range[0] || "…"} – ${range[1] || "…"}${facet?.unitSymbol ? ` ${facet.unitSymbol}` : ""}`
        : token);
      return {
        key: "attribute",
        value,
        label: `${facet?.name ?? (isEnglish ? "Attribute" : "Özellik")}: ${shownValue}`
      };
    })
  ].filter((item): item is { key: string; label: string; value?: string } =>
    item !== null);

  const baseQuery = baseFilterQuery(params, isEnglish);
  if (isEnglish) linkQuery.set("lang", "en");
  const sectionValue = one(params.section).trim();
  const brandValue = one(params.brand).trim();
  const tagValue = one(params.tag).trim();
  const sortValue = one(params.sort).trim() || "Name";
  const sectionOptions = buildOptions(baseQuery, "section", isEnglish ? "All groups" : "Tüm gruplar", navigation?.sections ?? [], sectionValue);
  const brandOptions = buildOptions(baseQuery, "brand", isEnglish ? "All brands" : "Tüm markalar", navigation?.brands ?? [], brandValue);
  const tagOptions = buildOptions(baseQuery, "tag", isEnglish ? "All tags" : "Tüm etiketler", navigation?.tags ?? [], tagValue);
  const sortOptions: FilterOption[] = [
    { value: "Name", label: isEnglish ? "Name A–Z" : "Ada göre A–Z" },
    { value: "NameDescending", label: isEnglish ? "Name Z–A" : "Ada göre Z–A" },
    { value: "Newest", label: isEnglish ? "Newest" : "En güncel" }
  ].map((item) => ({
    ...item,
    href: optionHref(baseQuery, "sort", item.value),
    selected: sortValue === item.value
  }));
  const searchValue = one(params.q).trim();

  return (
    <main className={styles.page} data-public-page>
      <PublicHeader currentPath={isEnglish ? "/en/products" : "/katalog"} />
      <h1 className={styles.srOnly}>{isEnglish ? "Product Catalogue" : "Ürün Kataloğu"}</h1>

      <div className={styles.catalogLayout}>
        <CatalogFilterForm className={styles.filters}>
          {(["section", "category", "brand", "tag", "sort"] as const).map((key) => {
            const value = one(params[key]).trim();
            return value ? <input key={key} type="hidden" name={key} value={value} /> : null;
          })}
          {attributeFilters
            .filter((value) => !value.slice(value.indexOf(":") + 1).startsWith("n:"))
            .map((value) => <input key={value} type="hidden" name="attribute" value={value} />)}
          {view === "list" && <input type="hidden" name="view" value="list" />}
          <details className={styles.filterPanel} open>
            <summary><span className={styles.filterLabel}>{isEnglish ? "Filters" : "Filtrele"}</span>{activeFilters.length > 0 && <span>{activeFilters.length} {isEnglish ? "active" : "aktif"}</span>}</summary>
            <div className={styles.filterDrawer}>
              <div className={styles.simpleFilterFields}>
                <details className={styles.filterGroup} open={Boolean(searchValue)}>
                  <summary><span>{isEnglish ? "Search products" : "Ürün ara"}</span></summary>
                  <div className={styles.field}>
                    <label htmlFor="catalog-search">{isEnglish ? "Search products" : "Ürün ara"}</label>
                    <input key={searchValue} id="catalog-search" name="q" type="search" minLength={2} maxLength={100} defaultValue={searchValue} placeholder={isEnglish ? "Product name or code" : "Ürün adı veya kodu"} />
                  </div>
                </details>
                <FilterOptionGroup title={isEnglish ? "Product group" : "Ürün grubu"} options={sectionOptions} />
                {navigation && <CatalogCategoryFilter categories={navigation.categories} selectedSlug={one(params.category).trim()} baseQuery={baseQuery} isEnglish={isEnglish} />}
                <FilterOptionGroup title="Marka" options={brandOptions} />
                <FilterOptionGroup title={isEnglish ? "Usage tag" : "Kullanım etiketi"} options={tagOptions} />
                <DynamicCatalogFilters facets={facets} selected={attributeFilters} baseQuery={baseQuery} />
                <FilterOptionGroup title={isEnglish ? "Sort" : "Sıralama"} options={sortOptions} open={Boolean(one(params.sort).trim())} />
                <div className={styles.filterActions}>
                  <button className={styles.primaryButton} type="submit">{isEnglish ? "Apply filters" : "Ara / uygula"}</button>
                  <Link className={styles.secondaryButton} href={basePath}>{isEnglish ? "Clear filters" : "Filtreleri temizle"}</Link>
                </div>
              </div>
            </div>
          </details>
        </CatalogFilterForm>

        <section className={styles.results} aria-labelledby="results-title">
          <h2 id="results-title" className={styles.srOnly}>Ürünler</h2>

          <div className={styles.catalogToolbar}>
            <span className={styles.productCount}>
              {products ? `${products.totalCount.toLocaleString(isEnglish ? "en-US" : "tr-TR")} ${isEnglish ? "products found" : "ürün bulundu"}` : (isEnglish ? "Products" : "Ürünler")}
            </span>
            <div className={styles.toolbarActions}>
              <div className={styles.viewActions} role="group" aria-label={isEnglish ? "View options" : "Görünüm seçimi"}>
                <Link className={styles.viewButton} href={viewHref(params, "grid", basePath)} aria-label={isEnglish ? "Grid view" : "Grid görünümü"} aria-current={view === "grid" ? "true" : undefined} scroll={false}><GridIcon /></Link>
                <Link className={styles.viewButton} href={viewHref(params, "list", basePath)} aria-label={isEnglish ? "List view" : "Liste görünümü"} aria-current={view === "list" ? "true" : undefined} scroll={false}><ListIcon /></Link>
              </div>
              <QuoteListIndicator className={styles.quoteLink} locale={isEnglish ? "en" : "tr"} />
            </div>
          </div>

          {activeFilters.length > 0 && (
            <div className={styles.activeFilters} aria-label={isEnglish ? "Active filters" : "Aktif filtreler"}>
              <span>{isEnglish ? "Active filters" : "Aktif filtreler"}</span>
              {activeFilters.map((filter) => (
                <Link
                  className={styles.activeFilter}
                  href={filterHref(linkQuery, filter.key, filter.value, basePath)}
                  key={`${filter.key}-${filter.value ?? ""}`}
                  aria-label={isEnglish ? `Remove ${filter.label} filter` : `${filter.label} filtresini kaldır`}
                >
                  {filter.label} <span aria-hidden="true">×</span>
                </Link>
              ))}
              <Link className={styles.clearFilters} href={basePath}>
                {isEnglish ? "Clear all" : "Tümünü temizle"}
              </Link>
            </div>
          )}

          {error ? (
            <div className={styles.error} role="alert">
              <strong>{isEnglish ? "Catalogue could not be loaded." : "Katalog yüklenemedi."}</strong>
              <p>{error}</p>
            </div>
          ) : products?.items.length === 0 ? (
            <div className={styles.empty}>
              <h3>{isEnglish ? "No products found with these filters." : "Bu filtrelerle ürün bulunamadı."}</h3>
              <p>{isEnglish ? "Try a shorter search term or clear the filters." : "Arama kelimenizi azaltın veya filtreleri temizleyin."}</p>
            </div>
          ) : (
            <div className={styles.productGrid} data-view={view}>
              {products?.items.map((product) => <CatalogProductCard key={product.id} product={product} locale={isEnglish ? "en" : "tr"} />)}
            </div>
          )}

          {products && pageCount > 1 && (
            <nav className={styles.pagination} aria-label="Katalog sayfaları">
              {products.page > 1 ? (
                <Link
                  className={styles.secondaryButton}
                  href={pageHref(linkQuery, products.page - 1, basePath)}
                >
                  {isEnglish ? "Previous" : "Önceki"}
                </Link>
              ) : (
                <span>{isEnglish ? "Previous" : "Önceki"}</span>
              )}
              <span>
                {isEnglish ? "Page" : "Sayfa"} {products.page} / {pageCount}
              </span>
              {products.page < pageCount ? (
                <Link
                  className={styles.secondaryButton}
                  href={pageHref(linkQuery, products.page + 1, basePath)}
                >
                  {isEnglish ? "Next" : "Sonraki"}
                </Link>
              ) : (
                <span>{isEnglish ? "Next" : "Sonraki"}</span>
              )}
            </nav>
          )}
        </section>
      </div>
    </main>
  );
}
