import Link from "next/link";
import { AddToQuoteButton } from "../../components/add-to-quote-button";
import type { CatalogProductSummary } from "../../lib/catalog-types";
import styles from "./catalog-list.module.css";

function initials(name: string): string {
  return name
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toLocaleUpperCase("tr-TR");
}

/** A consistent catalog card, shared by every product result. */
export function CatalogProductCard({ product }: { product: CatalogProductSummary }) {
  const productHref = `/katalog/${encodeURIComponent(product.slug)}`;

  return (
    <article className={styles.productCard}>
      <Link className={styles.productCardMain} href={productHref}>
        <div className={styles.productVisual} aria-hidden={product.image ? undefined : "true"}>
          {product.image ? (
            <img src={product.image.url} alt={product.image.altText} width={640} height={480} loading="lazy" />
          ) : (
            initials(product.name)
          )}
        </div>
        <div className={styles.productBody}>
          <span className={styles.meta}>{product.primaryCategory?.name ?? "Profesyonel ürün"}</span>
          <h3 title={product.name}>{product.name}</h3>
          <div className={styles.productIdentity}>
            <span>{product.brand?.name ?? "Ekiphan seçkisi"}</span>
            <span className={styles.sku}>Kod: {product.sku}</span>
          </div>
        </div>
      </Link>
      <div className={styles.productActions}>
        <Link className={styles.productDetailLink} href={productHref}>Detayı incele</Link>
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
  );
}
