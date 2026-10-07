import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { AddToQuoteButton } from "../../components/add-to-quote-button";
import { PublicHeader } from "../../components/public-header";
import { ProductGallery } from "../../components/product-gallery";
import { RelatedProductsCarousel } from "../../components/related-products-carousel";
import { ProductDetailSections } from "../../components/product-detail-sections";
import { CatalogApiError, getProduct } from "../../lib/catalog-api";
import type { CatalogAttribute, CatalogProductDetail, CatalogProductSummary } from "../../lib/catalog-types";
import catalogStyles from "./catalog.module.css";
import styles from "./product-detail.module.css";

export const dynamic = "force-dynamic";

function initials(name: string): string {
  return name.split(/\s+/).slice(0, 2).map((part) => part[0]).join("").toLocaleUpperCase("tr-TR");
}

function attributeValue(attribute: CatalogAttribute): string {
  if (attribute.optionName) return attribute.optionName;
  if (attribute.textValue) return attribute.textValue;
  if (attribute.numericValue !== null) {
    return `${attribute.numericValue.toLocaleString("tr-TR")}${attribute.unitSymbol ? ` ${attribute.unitSymbol}` : ""}`;
  }
  if (attribute.booleanValue !== null) return attribute.booleanValue ? "Evet" : "Hayır";
  return "—";
}

function uniqueProducts(products: CatalogProductSummary[]): CatalogProductSummary[] {
  return Array.from(new Map(products.map((product) => [product.id, product])).values()).slice(0, 12);
}

function specificationGroup(attribute: CatalogAttribute): string {
  const name = attribute.name.toLocaleLowerCase("tr-TR");
  if (/geniş|yüksek|derin|uzun|ölç|çap|ağırlık/.test(name)) return "Boyutlar";
  if (/volt|güç|frekans|elektr|enerji/.test(name)) return "Elektrik";
  if (/kapasite|hacim|porsiyon|adet/.test(name)) return "Kapasite";
  return "Ürün bilgileri";
}

function groupedAttributes(attributes: CatalogAttribute[]) {
  return attributes.reduce<Record<string, CatalogAttribute[]>>((groups, attribute) => {
    const group = specificationGroup(attribute);
    groups[group] = [...(groups[group] ?? []), attribute];
    return groups;
  }, {});
}

function RelatedProducts({ products, title, sectionId, locale = "tr" }: { products: CatalogProductSummary[]; title: string; sectionId: string; locale?: "tr" | "en" }) {
  if (products.length === 0) return null;
  const en = locale === "en";
  return <section className={styles.relatedSection} aria-labelledby={sectionId}><div className={styles.sectionHeading}><div><p className={styles.sectionEyebrow}>{en ? "RECOMMENDED PRODUCTS" : "ÜRÜN ÖNERİLERİ"}</p><h2 id={sectionId}>{title}</h2></div><Link href={en ? "/en/products" : "/katalog"}>{en ? "All Products" : "Tüm ürünler"} <span aria-hidden="true">→</span></Link></div><RelatedProductsCarousel items={products.map(product=>({id:product.id,slug:product.slug,name:product.name,sku:product.sku,category:product.primaryCategory?.name ?? "",brand:product.brand?.name ?? "",image:product.image}))} locale={locale}/></section>;
}
export async function generateMetadata({ params, searchParams }: {
  params: Promise<{ slug: string }>;
  searchParams: Promise<{ lang?: string }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const language = (await searchParams).lang === "en" ? "en" : "tr";
  const en = language === "en";
  try {
    const product = await getProduct(slug, language);
    const description = product.metaDescription ?? product.shortDescription ?? `${product.name} ürün özellikleri ve detayları.`;
    const path = `/katalog/${encodeURIComponent(product.slug)}`;
    return {
      title: product.metaTitle ?? product.name,
      description,
      alternates: {
        canonical: product.canonicalUrl ?? path,
        languages: Object.fromEntries((product.alternates ?? []).map((item) => [
          item.languageCode === "tr" ? "tr-TR" : "en",
          `/katalog/${encodeURIComponent(item.slug)}?lang=${item.languageCode}`
        ]))
      },
      robots: { index: !product.noIndex, follow: !product.noFollow },
      openGraph: {
        type: "website",
        locale: "tr_TR",
        title: product.openGraphTitle ?? product.metaTitle ?? product.name,
        description: product.openGraphDescription ?? description,
        url: product.canonicalUrl ?? path,
        images: product.openGraphImageUrl
          ? [{ url: product.openGraphImageUrl, alt: product.name }]
          : product.images.slice(0, 4).map((image) => ({ url: image.url, alt: image.altText }))
      }
    };
  } catch {
    return { title: "Ürün", robots: { index: false, follow: false } };
  }
}

function ErrorState({ caught, locale = "tr" }: { caught: unknown; locale?: "tr" | "en" }) {
  const en = locale === "en";
  return (
    <main className={`${catalogStyles.page} ${catalogStyles.detailPage}`} data-public-page>
      <PublicHeader currentPath={en ? "/en/products" : "/katalog"} />
      <section className={catalogStyles.hero}>
        <div><p className={catalogStyles.eyebrow}>{en ? "PRODUCT CATALOGUE" : "ÜRÜN KATALOĞU"}</p><h1>{en ? "Product information is unavailable." : "Ürün bilgisine ulaşılamadı."}</h1></div>
        <p className={catalogStyles.heroText}>{en ? "Product information will appear here once the connection is restored." : "Bağlantı yeniden kurulduğunda ürün bilgileri burada gösterilecek."}</p>
      </section>
      <Link className={catalogStyles.backLink} href={en ? "/en/products" : "/katalog"}>{en ? "← Back to Catalogue" : "← Kataloğa dön"}</Link>
      <div className={catalogStyles.error} role="alert"><strong>{en ? "Product information could not be loaded." : "Ürün bilgisi yüklenemedi."}</strong><p>{en ? "Please try again later." : caught instanceof CatalogApiError ? caught.message : "Lütfen daha sonra tekrar deneyin."}</p></div>
    </main>
  );
}

function quickSpecifications(product: CatalogProductDetail, en = false) {
  const fromAttributes = product.attributes.slice(0, 4).map((attribute) => ({ label: attribute.name, value: attributeValue(attribute) }));
  const fallbacks = [
    ...(product.categories[0] ? [{ label: en ? "Category" : "Kategori", value: product.categories[0].name }] : []),
    ...(product.brand ? [{ label: en ? "Brand" : "Marka", value: product.brand.name }] : []),
    { label: en ? "Product code" : "Ürün kodu", value: product.sku }
  ];
  return [...fromAttributes, ...fallbacks].slice(0, 4);
}

export default async function ProductDetailPage({ params, searchParams }: {
  params: Promise<{ slug: string }>;
  searchParams: Promise<{ lang?: string }>;
}) {
  const { slug } = await params;
  const language = (await searchParams).lang === "en" ? "en" : "tr";
  const en = language === "en";
  let product: CatalogProductDetail;
  try {
    product = await getProduct(slug, language);
  } catch (caught) {
    if (caught instanceof CatalogApiError && caught.status === 404) notFound();
    return <ErrorState caught={caught} locale={language} />;
  }

  const category = product.categories[0]?.name ?? "—";
  const specs = quickSpecifications(product, en);
  const groups = groupedAttributes(product.attributes);
  const similar = uniqueProducts(product.similarProducts);
  const complementary = uniqueProducts(product.complementaryProducts);
  const applicationAreas = product.attributes
    .filter(attribute => attribute.name.toLocaleLowerCase(en ? "en-US" : "tr-TR").includes(en ? "application area" : "kullanım alan"))
    .map(attributeValue).filter(value => value !== "—");
  const overviewItems = Array.from(new Set([
    ...product.tags.map((tag) => tag.name),
    ...product.categories.map((item) => item.name)
  ])).slice(0, 4);

  const detailSpecificationGroups = Object.entries(groups).map(([name, attributes]) => ({
    name,
    items: attributes.map((attribute) => ({
      id: attribute.id,
      name: attribute.name,
      value: attributeValue(attribute)
    }))
  }));

  return (
    <main className={styles.detailPage} data-public-page>
      <PublicHeader currentPath={en ? `/en/products/${encodeURIComponent(product.slug)}` : "/katalog"} />
      <div className={styles.pageShell}>
        <nav className={styles.breadcrumb} aria-label="Breadcrumb">
          <Link href={en ? "/en" : "/"}>{en ? "Home" : "Ana Sayfa"}</Link><span>/</span><Link href={en ? "/en/products" : "/katalog"}>{en ? "Products" : "Ürünler"}</Link><span>/</span>
          {product.categories[0] && <><Link href={`${en ? "/en/products" : "/katalog"}?category=${product.categories[0].slug}`}>{product.categories[0].name}</Link><span>/</span></>}
          <strong>{product.name}</strong>
        </nav>

        <section className={styles.productHero} aria-labelledby="product-title">
          <div className={styles.galleryColumn}><ProductGallery images={product.images} fallbackText={initials(product.name)} /></div>
          <div className={styles.productInfo}>
            <p className={styles.categoryLabel}>{category}</p>
            <div className={styles.brandLine}>
              {product.brand && <strong>{product.brand.name}</strong>}
            </div>
            <h1 id="product-title">{product.name}</h1>
            <p className={styles.modelLine}><span>{en ? "Product Code:" : "Ürün Kodu:"} {product.sku}</span></p>
            {product.shortDescription && <p className={styles.lead}>{product.shortDescription}</p>}
            <dl className={styles.quickSpecs}>{specs.map((spec) => <div key={`${spec.label}-${spec.value}`}><dt>{spec.label}</dt><dd>{spec.value}</dd></div>)}</dl>
            <div className={styles.primaryAction}>
              <AddToQuoteButton productId={product.id} slug={product.slug} name={product.name} sku={product.sku} brandName={product.brand?.name ?? null} imageUrl={product.images[0]?.url ?? null} className={styles.quoteButton} showQuantityControl quantityClassName={styles.quantityControl} locale={language} />
              <Link href={en ? "/en/contact" : "/iletisim"}>{en ? "Request a Quote" : "Teklif al"} <span aria-hidden="true">→</span></Link>
            </div>
            <div className={styles.secondaryActions}><Link href={en ? "/en/catalogs" : "/kataloglar"}>↓ {en ? "Product catalogue" : "Ürün kataloğu"}</Link><Link href={en ? "/en/contact" : "/iletisim"}>↓ {en ? "Request technical documents" : "Teknik doküman talep et"}</Link></div>
          </div>
        </section>
      </div>
      <ProductDetailSections
        productName={product.name}
        category={category}
        description={product.longDescription ?? product.shortDescription ?? ""}
        features={overviewItems}
        specificationGroups={detailSpecificationGroups}
        applicationAreas={applicationAreas}
        locale={language}
        afterOverview={<><RelatedProducts products={similar} title={en ? "Similar Products" : "Benzer ürünler"} sectionId="similar-products-title" locale={language} />
          <RelatedProducts products={complementary} title={en ? "Complementary Products" : "Tamamlayıcı ürünler"} sectionId="complementary-products-title" locale={language} /></>}
      />

      <div className={styles.mobileQuoteBar}>
        <span><small>{en ? "Add to your quote list" : "Teklif listenize ekleyin"}</small><strong>{product.name}</strong></span>
        <AddToQuoteButton productId={product.id} slug={product.slug} name={product.name} sku={product.sku} brandName={product.brand?.name ?? null} imageUrl={product.images[0]?.url ?? null} className={styles.mobileQuoteButton} locale={language} />
      </div>
    </main>
  );
}
