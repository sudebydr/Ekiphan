"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import type {
  AdminCatalogProduct,
  AdminCatalogProductPage,
  AdminProductRelation,
  ProblemDetails,
  ProductRelationType
} from "../../../../lib/admin-product-relation-types";
import {
  relationTypeOptions
} from "../../../../lib/admin-product-relation-types";
import styles from "./relations.module.css";

const pageSize = 20;

async function readError(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as ProblemDetails;
    return problem.detail ?? problem.title ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

function relationLabel(value: number): string {
  return relationTypeOptions.find((item) => item.numericValue === value)
    ?.label ?? "Bilinmeyen ilişki";
}

async function getProducts(search: string): Promise<AdminCatalogProductPage> {
  const query = new URLSearchParams({
    page: "1",
    pageSize: String(pageSize),
    language: "tr"
  });
  if (search.trim()) query.set("search", search.trim());
  const response = await fetch(`/api/admin/catalog/products?${query}`, {
    cache: "no-store"
  });
  if (!response.ok) throw new Error(await readError(response));
  return response.json() as Promise<AdminCatalogProductPage>;
}

export function ProductRelationAdminClient() {
  const [sourceSearch, setSourceSearch] = useState("");
  const [sourceProducts, setSourceProducts] =
    useState<AdminCatalogProductPage | null>(null);
  const [selectedProduct, setSelectedProduct] =
    useState<AdminCatalogProduct | null>(null);
  const [relations, setRelations] = useState<AdminProductRelation[]>([]);
  const [targetSearch, setTargetSearch] = useState("");
  const [targetProducts, setTargetProducts] =
    useState<AdminCatalogProductPage | null>(null);
  const [targetProductId, setTargetProductId] = useState("");
  const [relationType, setRelationType] =
    useState<ProductRelationType>("Similar");
  const [isBidirectional, setIsBidirectional] = useState(true);
  const [sortOrder, setSortOrder] = useState(0);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadSourceProducts = useCallback(async (search: string) => {
    setLoading(true);
    setError(null);
    try {
      setSourceProducts(await getProducts(search));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadSourceProducts("").catch((reason: unknown) => {
      setError(
        reason instanceof Error
          ? reason.message
          : "Ürün listesi alınamadı."
      );
    });
  }, [loadSourceProducts]);

  async function loadRelations(product: AdminCatalogProduct) {
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const response = await fetch(
        `/api/admin/catalog/products/${product.id}/relations?language=tr`,
        { cache: "no-store" }
      );
      if (!response.ok) throw new Error(await readError(response));
      setSelectedProduct(product);
      setRelations((await response.json()) as AdminProductRelation[]);
      setTargetProductId("");
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "Ürün ilişkileri alınamadı."
      );
    } finally {
      setBusy(false);
    }
  }

  async function searchSources(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await loadSourceProducts(sourceSearch).catch((reason: unknown) => {
      setError(
        reason instanceof Error ? reason.message : "Ürün araması başarısız."
      );
    });
  }

  async function searchTargets() {
    setBusy(true);
    setError(null);
    try {
      const result = await getProducts(targetSearch);
      setTargetProducts(result);
      setTargetProductId(
        result.items.find((item) => item.id !== selectedProduct?.id)?.id ?? ""
      );
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "Hedef ürün araması başarısız."
      );
    } finally {
      setBusy(false);
    }
  }

  async function refreshRelations() {
    if (selectedProduct) await loadRelations(selectedProduct);
  }

  async function createRelation(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedProduct || !targetProductId) return;
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const response = await fetch(
        `/api/admin/catalog/products/${selectedProduct.id}/relations?language=tr`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            targetProductId,
            relationType,
            isBidirectional,
            sortOrder
          })
        }
      );
      if (!response.ok) throw new Error(await readError(response));
      setMessage("Ürün ilişkisi kaydedildi.");
      await refreshRelations();
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "Ürün ilişkisi kaydedilemedi."
      );
    } finally {
      setBusy(false);
    }
  }

  async function deactivateRelation(relationId: string) {
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const response = await fetch(
        `/api/admin/catalog/relations/${relationId}`,
        { method: "DELETE" }
      );
      if (!response.ok) throw new Error(await readError(response));
      setMessage("Ürün ilişkisi pasifleştirildi.");
      await refreshRelations();
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "Ürün ilişkisi pasifleştirilemedi."
      );
    } finally {
      setBusy(false);
    }
  }

  const availableTargets =
    targetProducts?.items.filter(
      (product) => product.id !== selectedProduct?.id
    ) ?? [];

  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <div>
          <p className={styles.eyebrow}>EKİPHAN · ADMIN</p>
          <h1>Ürün ilişkileri</h1>
          <p className={styles.lead}>
            Benzer, tamamlayıcı, aksesuar ve alternatif ürün bağlantılarını
            manuel olarak yönetin.
          </p>
        </div>
        <nav className={styles.nav} aria-label="Admin menüsü">
          <a href="/admin/catalog/products">Ürün yönetimi</a>
          <a href="/admin/catalog/categories">Kategoriler</a>
          <a href="/admin/catalog/brands">Markalar</a>
          <a href="/admin/imports">Ürün importu</a>
          <a href="/admin/quotes">Teklifler</a>
          <a href="/">Siteye dön</a>
        </nav>
      </header>

      {error && <div className={styles.error} role="alert">{error}</div>}
      {message && (
        <div className={styles.success} role="status">{message}</div>
      )}

      <section className={styles.panel} aria-labelledby="source-title">
        <div>
          <p className={styles.sectionIndex}>01</p>
          <h2 id="source-title">Kaynak ürünü seçin</h2>
        </div>
        <form className={styles.searchForm} onSubmit={searchSources}>
          <label htmlFor="source-search">Ürün adı veya SKU</label>
          <div>
            <input
              id="source-search"
              type="search"
              maxLength={100}
              value={sourceSearch}
              onChange={(event) => setSourceSearch(event.target.value)}
            />
            <button type="submit" disabled={busy}>Ara</button>
          </div>
        </form>
        <div className={styles.productList} aria-busy={loading}>
          {sourceProducts?.items.map((product) => (
            <button
              className={
                selectedProduct?.id === product.id
                  ? styles.selectedProduct
                  : styles.productButton
              }
              type="button"
              key={product.id}
              disabled={busy}
              onClick={() => void loadRelations(product)}
            >
              <strong>{product.name}</strong>
              <span>{product.sku}</span>
              <small>{product.isPublished ? "Yayında" : "Taslak"}</small>
            </button>
          ))}
          {!loading && sourceProducts?.items.length === 0 && (
            <p>Aramayla eşleşen ürün bulunamadı.</p>
          )}
        </div>
      </section>

      {selectedProduct && (
        <>
          <section className={styles.panel} aria-labelledby="relations-title">
            <div className={styles.sectionHeader}>
              <div>
                <p className={styles.sectionIndex}>02</p>
                <h2 id="relations-title">
                  {selectedProduct.name} ilişkileri
                </h2>
              </div>
              <span>{selectedProduct.sku}</span>
            </div>
            <div className={styles.tableWrap}>
              <table>
                <thead>
                  <tr>
                    <th>İlişkili ürün</th>
                    <th>Tür</th>
                    <th>Yön</th>
                    <th>Sıra</th>
                    <th><span className={styles.srOnly}>İşlem</span></th>
                  </tr>
                </thead>
                <tbody>
                  {relations.map((relation) => (
                    <tr key={relation.id}>
                      <td>
                        <strong>{relation.relatedName}</strong>
                        <br />
                        <small>{relation.relatedSKU}</small>
                      </td>
                      <td>{relationLabel(relation.relationType)}</td>
                      <td>
                        {relation.isIncoming
                          ? "Ters yönden"
                          : relation.isBidirectional
                            ? "Çift yönlü"
                            : "Tek yönlü"}
                      </td>
                      <td>{relation.sortOrder}</td>
                      <td>
                        <button
                          className={styles.textButton}
                          type="button"
                          disabled={busy}
                          onClick={() => void deactivateRelation(relation.id)}
                        >
                          Pasifleştir
                        </button>
                      </td>
                    </tr>
                  ))}
                  {relations.length === 0 && (
                    <tr>
                      <td colSpan={5}>Aktif manuel ilişki bulunmuyor.</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </section>

          <section className={styles.panel} aria-labelledby="create-title">
            <div>
              <p className={styles.sectionIndex}>03</p>
              <h2 id="create-title">Yeni ilişki ekleyin</h2>
            </div>
            <form className={styles.relationForm} onSubmit={createRelation}>
              <div className={styles.targetSearch}>
                <label htmlFor="target-search">Hedef ürün araması</label>
                <div>
                  <input
                    id="target-search"
                    type="search"
                    maxLength={100}
                    value={targetSearch}
                    onChange={(event) => setTargetSearch(event.target.value)}
                  />
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => void searchTargets()}
                  >
                    Hedefleri getir
                  </button>
                </div>
              </div>
              <label>
                Hedef ürün
                <select
                  required
                  value={targetProductId}
                  onChange={(event) => setTargetProductId(event.target.value)}
                >
                  <option value="">Ürün seçin</option>
                  {availableTargets.map((product) => (
                    <option key={product.id} value={product.id}>
                      {product.name} · {product.sku}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                İlişki türü
                <select
                  value={relationType}
                  onChange={(event) =>
                    setRelationType(event.target.value as ProductRelationType)
                  }
                >
                  {relationTypeOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Yönetim sırası
                <input
                  type="number"
                  value={sortOrder}
                  onChange={(event) => setSortOrder(event.target.valueAsNumber)}
                />
              </label>
              <label className={styles.checkbox}>
                <input
                  type="checkbox"
                  checked={isBidirectional}
                  onChange={(event) => setIsBidirectional(event.target.checked)}
                />
                İlişkiyi iki üründe de göster
              </label>
              <button type="submit" disabled={busy || !targetProductId}>
                {busy ? "Kaydediliyor…" : "İlişkiyi kaydet"}
              </button>
            </form>
          </section>
        </>
      )}
    </main>
  );
}
