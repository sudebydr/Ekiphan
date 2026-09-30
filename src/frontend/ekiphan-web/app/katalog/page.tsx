import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../../components/public-header";
import { AddToQuoteButton } from "../../components/add-to-quote-button";
import { QuoteListIndicator } from "../../components/quote-list-indicator";
import { PublicNavigation } from "../../components/public-navigation";
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
import styles from "./catalog.module.css";

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

function initials(name: string): string {
  return name
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toLocaleUpperCase("tr-TR");
}

export default async function CatalogPage({
  searchParams
}: {
  searchParams: Promise<SearchParams>;
}) {
  const params = await searchParams;
  const query = productQuery(params);
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

  const selectedCategories = many(params.category);
  const selectedBrands = many(params.brand);
  const selectedTags = many(params.tag);
  const selectedColors = many(params.color);
  const selectedSizes = many(params.size);
  const categoryRoots = navigation?.categories.filter((item) => item.parentId === null) ?? [];
  const subcategories = navigation?.categories.filter((item) => item.parentId !== null) ?? [];

  return (
    <main className={`${styles.page} ${styles.catalogPage}`} data-public-page>
      <PublicHeader currentPath="/katalog" />

      <div className={styles.catalogLayout}>
        <CatalogFilterForm className={styles.filters}>
          <details className={styles.filterPanel} open>
            <summary><span className={styles.filterLabel}>Filtrele</span>{activeFilters.length > 0 && <span>{activeFilters.length} aktif</span>}</summary>
            <div className={styles.filterDrawer}>
              <div className={styles.simpleFilterFields}>
                <div className={styles.field}>
                  <label htmlFor="catalog-search">{"\u00dcr\u00fcn ara"}</label>
                  <input id="catalog-search" name="q" type="search" minLength={2} maxLength={100} defaultValue={one(params.q)} placeholder={"\u00dcr\u00fcn ad\u0131 veya kodu"} />
                </div>
                <div className={styles.field}>
                  <label htmlFor="section">{"\u00dcr\u00fcn grubu"}</label>
                  <select id="section" name="section" defaultValue={one(params.section)}>
                    <option value="">{"T\u00fcm gruplar"}</option>
                    {navigation?.sections.map((item) => <option key={item.id} value={item.slug}>{item.name}</option>)}
                  </select>
                </div>
                {navigation && <CatalogCategoryFilter categories={navigation.categories} selectedSlug={one(params.category)} />}
                <div className={styles.field}>
                  <label htmlFor="brand">Marka</label>
                  <select id="brand" name="brand" defaultValue={one(params.brand)}>
                    <option value="">{"T\u00fcm markalar"}</option>
                    {navigation?.brands.filter((item) => item.slug).map((item) => <option key={item.id} value={item.slug ?? ""}>{item.name}</option>)}
                  </select>
                </div>
                <div className={styles.field}>
                  <label htmlFor="tag">{"Kullan\u0131m etiketi"}</label>
                  <select id="tag" name="tag" defaultValue={one(params.tag)}>
                    <option value="">{"T\u00fcm etiketler"}</option>
                    {navigation?.tags.map((item) => <option key={item.id} value={item.slug}>{item.name}</option>)}
                  </select>
                </div>
                <DynamicCatalogFilters facets={facets} selected={attributeFilters} />
                <div className={styles.field}>
                  <label htmlFor="sort">{"S\u0131ralama"}</label>
                  <select id="sort" name="sort" defaultValue={one(params.sort)}>
                    <option value="Name">{"Ada g\u00f6re A\u2013Z"}</option>
                    <option value="NameDescending">{"Ada g\u00f6re Z\u2013A"}</option>
                    <option value="Newest">{"En g\u00fcncel"}</option>
                  </select>
                </div>
                <div className={styles.filterActions}>
                  <button className={styles.primaryButton} type="submit">{"Sonu\u00e7lar\u0131 g\u00f6ster"}</button>
                  <button className={styles.secondaryButton} type="reset">Filtreleri temizle</button>
                </div>
              </div>
            </div>
          </details>
        </CatalogFilterForm>
        <div className={styles.catalogToolbar}>
          <span className={styles.productCount}>{products ? `${products.totalCount.toLocaleString("tr-TR")} \u00fcr\u00fcn bulundu` : "\u00dcr\u00fcnler"}</span>
          <div className={styles.viewActions} aria-label="G\u00f6r\u00fcn\u00fcm se\u00e7imi">
            <button type="button" className={styles.viewButton} aria-label="Grid g\u00f6r\u00fcn\u00fcm\u00fc" aria-pressed="true">{"▦"}</button>
            <button type="button" className={styles.viewButton} aria-label="Liste g\u00f6r\u00fcn\u00fcm\u00fc" aria-pressed="false">{"☷"}</button>
          </div>
        </div>
        <section aria-labelledby="results-title">
          <div className={styles.resultsHeader}>
            <h2 id="results-title">Ürünler</h2>
            <p>
              {products
                ? `${products.totalCount.toLocaleString("tr-TR")} ürün`
                : "Sonuç alınamadı"}
            </p>
          </div>

          {activeFilters.length > 0 && (
            <div className={styles.activeFilters} aria-label="Aktif filtreler">
              <span>Aktif filtreler</span>
              {activeFilters.map((filter) => (
                <Link
                  className={styles.activeFilter}
                  href={filterHref(query, filter.key, filter.value)}
                  key={filter.key}
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
            <div className={styles.productGrid}>
              {products?.items.map((product) => (
                <article className={styles.productCard} key={product.id}>
                  <Link className={styles.productCardMain} href={`/katalog/${encodeURIComponent(product.slug)}`}>
                  <div
                    className={styles.productVisual}
                    aria-hidden={product.image ? undefined : "true"}
                  >
                    {product.image ? (
                      <img
                        src={product.image.url}
                        alt={product.image.altText}
                        width={640}
                        height={480}
                        loading="lazy"
                      />
                    ) : (
                      initials(product.name)
                    )}
                  </div>
                  <div className={styles.productBody}>
                    <span className={styles.meta}>
                      {product.primaryCategory?.name ??
                        product.brand?.name ??
                        "Profesyonel ürün"}
                    </span>
                    <h3>{product.name}</h3>
                    {product.brand && <p className={styles.productBrand}>{product.brand.name}</p>}
                    {product.shortDescription && (
                      <p>{product.shortDescription}</p>
                    )}
                    <span className={styles.sku}>Kod: {product.sku}</span>
                  </div>
                  </Link>
                  <div className={styles.productActions}>
                      <Link
                        className={styles.productDetailLink}
                        href={`/katalog/${encodeURIComponent(product.slug)}`}
                      >
                        {"Detay\u0131 \u0130ncele \u2192"}
                      </Link>
                      <AddToQuoteButton
                        productId={product.id}
                        slug={product.slug}
                        name={product.name}
                        sku={product.sku}
                        brandName={product.brand?.name ?? null}
                        imageUrl={product.image?.url ?? null}
                        className={styles.productQuoteButton}
                        showQuantityControl
                        quantityClassName={styles.quoteQuantity}
                        compactQuantityControl
                      />
                    </div>
                </article>
              ))}
            </div>
          )}

          {products && pageCount > 1 && (
            <nav className={styles.pagination} aria-label="Katalog sayfaları">
              {products.page > 1 ? (
                <Link
                  className={styles.secondaryButton}
                  href={pageHref(query, products.page - 1)}
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
                  href={pageHref(query, products.page + 1)}
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
      <QuoteListIndicator className={styles.floatingQuoteButton} />
    </main>
  );
}
