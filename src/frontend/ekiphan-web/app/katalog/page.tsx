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
  const categorySlug = one(params.category).trim();
  if (categorySlug) {
    try {
      const navigation = await getCatalogNavigation();
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
  return { title: "Ürün Kataloğu", description: "Ekiphan otel, restoran ve endüstriyel mutfak ekipmanları kataloğu.", alternates: { canonical: "/katalog" }, openGraph: { type: "website", locale: "tr_TR", title: "Ürün Kataloğu", description: "Ekiphan otel, restoran ve endüstriyel mutfak ekipmanları kataloğu.", images: defaultSocialImage ? [defaultSocialImage] : undefined, url: "/katalog" } };
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

function pageHref(query: URLSearchParams, page: number): string {
  const next = new URLSearchParams(query);
  next.set("page", String(page));
  return `/katalog?${next.toString()}`;
}

function filterHref(query: URLSearchParams, key: string, value?: string): string {
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
  return queryString ? `/katalog?${queryString}` : "/katalog";
}

/** Filtre bağlantılarının temeli: şu anki tüm seçimler (sayfa hariç). */
function baseFilterQuery(params: SearchParams): string {
  const query = new URLSearchParams();
  for (const key of ["q", "section", "category", "brand", "tag", "sort"]) {
    const value = one(params[key]).trim();
    if (value) query.set(key, value);
  }
  for (const value of many(params.attribute)) query.append("attribute", value);
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

function viewHref(params: SearchParams, view: CatalogView): string {
  const next = new URLSearchParams();
  for (const [key, raw] of Object.entries(params)) {
    if (key === "view" || key === "page") continue;
    for (const value of Array.isArray(raw) ? raw : raw ? [raw] : []) {
      if (value) next.append(key, value);
    }
  }
  if (view === "list") next.set("view", "list");
  const queryString = next.toString();
  return queryString ? `/katalog?${queryString}` : "/katalog";
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
      getCatalogNavigation(),
      getProducts(query)
    ]);
    const category = one(params.category).trim();
    if (category) facets = await getCatalogFacets(category);
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
      ? { key: "q", label: `Arama: ${one(params.q).trim()}` }
      : null,
    one(params.section).trim()
      ? {
          key: "section",
          label: `Grup: ${
            navigation?.sections.find(
              (item) => item.slug === one(params.section).trim()
            )?.name ?? one(params.section).trim()
          }`
        }
      : null,
    one(params.category).trim()
      ? {
          key: "category",
          label: `Kategori: ${
            navigation?.categories.find(
              (item) => item.slug === one(params.category).trim()
            )?.name ?? one(params.category).trim()
          }`
        }
      : null,
    one(params.brand).trim()
      ? {
          key: "brand",
          label: `Marka: ${
            navigation?.brands.find(
              (item) => item.slug === one(params.brand).trim()
            )?.name ?? one(params.brand).trim()
          }`
        }
      : null,
    one(params.tag).trim()
      ? {
          key: "tag",
          label: `Etiket: ${
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
        label: `${facet?.name ?? "Özellik"}: ${shownValue}`
      };
    })
  ].filter((item): item is { key: string; label: string; value?: string } =>
    item !== null);

  const baseQuery = baseFilterQuery(params);
  const sectionValue = one(params.section).trim();
  const brandValue = one(params.brand).trim();
  const tagValue = one(params.tag).trim();
  const sortValue = one(params.sort).trim() || "Name";
  const sectionOptions = buildOptions(baseQuery, "section", "Tüm gruplar", navigation?.sections ?? [], sectionValue);
  const brandOptions = buildOptions(baseQuery, "brand", "Tüm markalar", navigation?.brands ?? [], brandValue);
  const tagOptions = buildOptions(baseQuery, "tag", "Tüm etiketler", navigation?.tags ?? [], tagValue);
  const sortOptions: FilterOption[] = [
    { value: "Name", label: "Ada göre A–Z" },
    { value: "NameDescending", label: "Ada göre Z–A" },
    { value: "Newest", label: "En güncel" }
  ].map((item) => ({
    ...item,
    href: optionHref(baseQuery, "sort", item.value),
    selected: sortValue === item.value
  }));
  const searchValue = one(params.q).trim();

  return (
    <main className={styles.page} data-public-page>
      <PublicHeader currentPath="/katalog" />
      <h1 className={styles.srOnly}>Ürün Kataloğu</h1>

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
            <summary><span className={styles.filterLabel}>Filtrele</span>{activeFilters.length > 0 && <span>{activeFilters.length} aktif</span>}</summary>
            <div className={styles.filterDrawer}>
              <div className={styles.simpleFilterFields}>
                <details className={styles.filterGroup} open={Boolean(searchValue)}>
                  <summary><span>Ürün ara</span></summary>
                  <div className={styles.field}>
                    <label htmlFor="catalog-search">Ürün ara</label>
                    <input key={searchValue} id="catalog-search" name="q" type="search" minLength={2} maxLength={100} defaultValue={searchValue} placeholder="Ürün adı veya kodu" />
                  </div>
                </details>
                <FilterOptionGroup title="Ürün grubu" options={sectionOptions} />
                {navigation && <CatalogCategoryFilter categories={navigation.categories} selectedSlug={one(params.category).trim()} baseQuery={baseQuery} />}
                <FilterOptionGroup title="Marka" options={brandOptions} />
                <FilterOptionGroup title="Kullanım etiketi" options={tagOptions} />
                <DynamicCatalogFilters facets={facets} selected={attributeFilters} baseQuery={baseQuery} />
                <FilterOptionGroup title="Sıralama" options={sortOptions} open={Boolean(one(params.sort).trim())} />
                <div className={styles.filterActions}>
                  <button className={styles.primaryButton} type="submit">Ara / uygula</button>
                  <Link className={styles.secondaryButton} href="/katalog">Filtreleri temizle</Link>
                </div>
              </div>
            </div>
          </details>
        </CatalogFilterForm>

        <section className={styles.results} aria-labelledby="results-title">
          <h2 id="results-title" className={styles.srOnly}>Ürünler</h2>

          <div className={styles.catalogToolbar}>
            <span className={styles.productCount}>
              {products ? `${products.totalCount.toLocaleString("tr-TR")} ürün bulundu` : "Ürünler"}
            </span>
            <div className={styles.toolbarActions}>
              <div className={styles.viewActions} role="group" aria-label="Görünüm seçimi">
                <Link className={styles.viewButton} href={viewHref(params, "grid")} aria-label="Grid görünümü" aria-current={view === "grid" ? "true" : undefined} scroll={false}><GridIcon /></Link>
                <Link className={styles.viewButton} href={viewHref(params, "list")} aria-label="Liste görünümü" aria-current={view === "list" ? "true" : undefined} scroll={false}><ListIcon /></Link>
              </div>
              <QuoteListIndicator className={styles.quoteLink} />
            </div>
          </div>

          {activeFilters.length > 0 && (
            <div className={styles.activeFilters} aria-label="Aktif filtreler">
              <span>Aktif filtreler</span>
              {activeFilters.map((filter) => (
                <Link
                  className={styles.activeFilter}
                  href={filterHref(linkQuery, filter.key, filter.value)}
                  key={`${filter.key}-${filter.value ?? ""}`}
                  aria-label={`${filter.label} filtresini kaldır`}
                >
                  {filter.label} <span aria-hidden="true">×</span>
                </Link>
              ))}
              <Link className={styles.clearFilters} href="/katalog">
                Tümünü temizle
              </Link>
            </div>
          )}

          {error ? (
            <div className={styles.error} role="alert">
              <strong>Katalog yüklenemedi.</strong>
              <p>{error}</p>
            </div>
          ) : products?.items.length === 0 ? (
            <div className={styles.empty}>
              <h3>Bu filtrelerle ürün bulunamadı.</h3>
              <p>Arama kelimenizi azaltın veya filtreleri temizleyin.</p>
            </div>
          ) : (
            <div className={styles.productGrid} data-view={view}>
              {products?.items.map((product) => <CatalogProductCard key={product.id} product={product} />)}
            </div>
          )}

          {products && pageCount > 1 && (
            <nav className={styles.pagination} aria-label="Katalog sayfaları">
              {products.page > 1 ? (
                <Link
                  className={styles.secondaryButton}
                  href={pageHref(linkQuery, products.page - 1)}
                >
                  Önceki
                </Link>
              ) : (
                <span>Önceki</span>
              )}
              <span>
                Sayfa {products.page} / {pageCount}
              </span>
              {products.page < pageCount ? (
                <Link
                  className={styles.secondaryButton}
                  href={pageHref(linkQuery, products.page + 1)}
                >
                  Sonraki
                </Link>
              ) : (
                <span>Sonraki</span>
              )}
            </nav>
          )}
        </section>
      </div>
    </main>
  );
}
