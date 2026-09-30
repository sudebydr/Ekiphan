"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import type {
  AdminCatalogProduct,
  AdminCatalogProductPage,
  ProblemDetails
} from "../../../../lib/admin-product-relation-types";
import type {
  AdminProductDetail,
  AdminProductAttributeValue,
  AdminProductTranslation,
  SaveAdminProduct
} from "../../../../lib/admin-product-types";
import type { AdminBrandPage } from "../../../../lib/admin-brand-types";
import type { AdminCatalogStructure } from "../../../../lib/admin-category-types";
import type { AdminAttributeCatalog } from "../../../../lib/admin-attribute-types";
import type { AdminDictionaryCatalog } from "../../../../lib/admin-dictionary-types";
import type {
  ProductBulkExecution,
  ProductBulkOperation,
  ProductBulkPreview,
  ProductBulkSelection
} from "../../../../lib/admin-product-bulk-types";
import styles from "./products.module.css";

type TranslationDraft = {
  name: string;
  slug: string;
  shortDescription: string;
  longDescription: string;
  metaTitle: string;
  metaDescription: string;
  canonicalUrl: string;
  noIndex: boolean;
  noFollow: boolean;
  openGraphTitle: string;
  openGraphDescription: string;
  openGraphImageMediaId: string | null;
};

const emptyTranslation: TranslationDraft = {
  name: "",
  slug: "",
  shortDescription: "",
  longDescription: "",
  metaTitle: "",
  metaDescription: "",
  canonicalUrl: "",
  noIndex: false,
  noFollow: false,
  openGraphTitle: "",
  openGraphDescription: "",
  openGraphImageMediaId: null
};

async function readError(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as ProblemDetails;
    return problem.detail ?? problem.title ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function ProductAdminClient() {
  const [products, setProducts] = useState<AdminCatalogProduct[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [brands, setBrands] = useState<AdminBrandPage | null>(null);
  const [structure, setStructure] = useState<AdminCatalogStructure | null>(null);
  const [attributeCatalog, setAttributeCatalog] = useState<AdminAttributeCatalog | null>(null);
  const [dictionary, setDictionary] = useState<AdminDictionaryCatalog | null>(null);
  const [search, setSearch] = useState("");
  const [brandFilter, setBrandFilter] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [publicationFilter, setPublicationFilter] = useState("");
  const [missingImageFilter, setMissingImageFilter] = useState(false);
  const [missingEnglishFilter, setMissingEnglishFilter] = useState(false);
  const [sortBy, setSortBy] = useState("updatedAt");
  const [sortDirection, setSortDirection] = useState("desc");
  const [listLoading, setListLoading] = useState(true);
  const [dirty, setDirty] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [brandId, setBrandId] = useState<string | null>(null);
  const [categoryIds, setCategoryIds] = useState<string[]>([]);
  const [primaryCategoryId, setPrimaryCategoryId] = useState<string | null>(null);
  const [tagIds, setTagIds] = useState<string[]>([]);
  const [attributeValues, setAttributeValues] = useState<Record<string, string[]>>({});
  const [attributeUnits, setAttributeUnits] = useState<Record<string, string>>({});
  const [sku, setSku] = useState("");
  const [isPublished, setIsPublished] = useState(false);
  const [tr, setTr] = useState<TranslationDraft>(emptyTranslation);
  const [en, setEn] = useState<TranslationDraft>(emptyTranslation);
  const [includeEnglish, setIncludeEnglish] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [bulkBusy, setBulkBusy] = useState(false);
  const [bulkPreview, setBulkPreview] = useState<ProductBulkPreview | null>(null);
  const [bulkTargetCount, setBulkTargetCount] = useState(0);
  const [bulkOperation, setBulkOperation] = useState<ProductBulkOperation | null>(null);
  const [bulkError, setBulkError] = useState<string | null>(null);

  const loadProducts = useCallback(async (term = "", targetPage = 1) => {
    const query = new URLSearchParams({
      page: String(targetPage),
      pageSize: String(pageSize),
      language: "tr",
      sortBy,
      sortDirection
    });
    if (term.trim()) query.set("search", term.trim());
    if (brandFilter) query.set("brandId", brandFilter);
    if (categoryFilter) query.set("categoryId", categoryFilter);
    if (publicationFilter) query.set("isPublished", publicationFilter);
    if (missingImageFilter) query.set("missingImage", "true");
    if (missingEnglishFilter) query.set("missingEnglish", "true");
    setListLoading(true);
    try {
      const response = await fetch(`/api/admin/catalog/products?${query}`, {
        cache: "no-store"
      });
      if (!response.ok) throw new Error(await readError(response));
      const result = (await response.json()) as AdminCatalogProductPage;
      setProducts(result.items);
      setPage(result.page);
      setTotalCount(result.totalCount);
    } finally {
      setListLoading(false);
    }
  }, [
    brandFilter,
    categoryFilter,
    missingEnglishFilter,
    missingImageFilter,
    pageSize,
    publicationFilter,
    sortBy,
    sortDirection
  ]);

  useEffect(() => {
    void loadProducts().catch((reason: unknown) =>
      setError(reason instanceof Error ? reason.message : "Ürünler alınamadı.")
    );
    void fetch(
      "/api/admin/catalog/brands?page=1&pageSize=100&language=tr",
      { cache: "no-store" }
    ).then(async (response) => {
      if (!response.ok) throw new Error(await readError(response));
      setBrands((await response.json()) as AdminBrandPage);
    }).catch((reason: unknown) =>
      setError(reason instanceof Error ? reason.message : "Markalar alınamadı.")
    );
    void fetch("/api/admin/catalog/structure", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) throw new Error(await readError(response));
        setStructure((await response.json()) as AdminCatalogStructure);
      }).catch((reason: unknown) =>
      setError(reason instanceof Error ? reason.message : "Kategoriler alınamadı.")
      );
    void fetch("/api/admin/catalog/attributes", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) throw new Error(await readError(response));
        setAttributeCatalog((await response.json()) as AdminAttributeCatalog);
      }).catch((reason: unknown) =>
        setError(reason instanceof Error ? reason.message : "Özellikler alınamadı.")
      );
    void fetch("/api/admin/catalog/dictionaries", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) throw new Error(await readError(response));
        setDictionary((await response.json()) as AdminDictionaryCatalog);
      }).catch((reason: unknown) =>
        setError(reason instanceof Error ? reason.message : "Etiketler alınamadı.")
      );
  }, [loadProducts]);

  useEffect(() => {
    if (!dirty) return;
    const warn = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  useEffect(() => {
    if (!bulkOperation || isBulkOperationComplete(bulkOperation.status)) return;

    let cancelled = false;
    let timer: ReturnType<typeof setTimeout> | undefined;
    const poll = async () => {
      try {
        const response = await fetch(
          `/api/admin/products/bulk-operations/${bulkOperation.operationId}`,
          { cache: "no-store" }
        );
        if (!response.ok) throw new Error(await readError(response));
        const next = (await response.json()) as ProductBulkOperation;
        if (cancelled) return;
        setBulkOperation(next);
        if (!isBulkOperationComplete(next.status)) {
          timer = setTimeout(() => void poll(), 5_000);
        }
      } catch (reason) {
        if (!cancelled) {
          setBulkError(reason instanceof Error ? reason.message : "Toplu işlem durumu alınamadı.");
        }
      }
    };

    void poll();
    return () => {
      cancelled = true;
      if (timer) clearTimeout(timer);
    };
  }, [bulkOperation?.operationId, bulkOperation?.status]);

  const canBulkPublish = publicationFilter === "false" && Boolean(brandFilter || search.trim());

  async function collectFilteredProductIds(): Promise<string[]> {
    const query = new URLSearchParams({ language: "tr", isPublished: "false" });
    if (search.trim()) query.set("search", search.trim());
    if (brandFilter) query.set("brandId", brandFilter);
    if (categoryFilter) query.set("categoryId", categoryFilter);
    if (missingImageFilter) query.set("missingImage", "true");
    if (missingEnglishFilter) query.set("missingEnglish", "true");

    const response = await fetch(`/api/admin/products/bulk/selection?${query}`, {
      cache: "no-store"
    });
    if (!response.ok) throw new Error(await readError(response));
    const selection = (await response.json()) as ProductBulkSelection;
    const productIds = [...new Set(selection.productIds)];
    if (productIds.length !== selection.productIds.length || productIds.length > 5_000) {
      throw new Error("Toplu yayınlama için geçersiz ürün seçimi döndürüldü.");
    }
    return productIds;
  }

  async function previewBulkPublish() {
    if (!canBulkPublish) return;
    setBulkBusy(true);
    setBulkError(null);
    setBulkOperation(null);
    setBulkPreview(null);
    try {
      const productIds = await collectFilteredProductIds();
      if (productIds.length === 0) {
        throw new Error("Mevcut filtrelere uygun pasif ürün bulunamadı.");
      }

      const response = await fetch("/api/admin/products/bulk/preview", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          operationType: 1,
          productIds,
          parameters: null,
          reason: "Filtrelenen pasif ürünlerin toplu yayınlama önizlemesi"
        })
      });
      if (!response.ok) throw new Error(await readError(response));
      setBulkTargetCount(productIds.length);
      setBulkPreview((await response.json()) as ProductBulkPreview);
    } catch (reason) {
      setBulkError(reason instanceof Error ? reason.message : "Toplu yayınlama önizlemesi alınamadı.");
    } finally {
      setBulkBusy(false);
    }
  }

  async function executeBulkPublish() {
    if (!bulkPreview || bulkPreview.affectedProductIds.length === 0) return;
    if (!window.confirm(`${bulkPreview.eligibleProducts} ürünü arka planda yayınlamak istediğinizi onaylıyor musunuz?`)) {
      return;
    }

    setBulkBusy(true);
    setBulkError(null);
    try {
      const response = await fetch("/api/admin/products/bulk", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          operationType: 1,
          productIds: bulkPreview.affectedProductIds,
          parameters: null,
          reason: "Filtrelenen pasif ürünlerin toplu yayınlanması",
          executeAsBackgroundJob: true,
          previewToken: bulkPreview.previewToken
        })
      });
      if (!response.ok) throw new Error(await readError(response));
      const execution = (await response.json()) as ProductBulkExecution;
      setBulkOperation({
        operationId: execution.bulkOperationId,
        operationType: 1,
        status: execution.status,
        totalCount: execution.totalCount,
        successCount: execution.successCount,
        failedCount: execution.failedCount,
        skippedCount: execution.skippedCount,
        progressPercentage: 0,
        errorMessage: execution.errorMessage
      });
      setBulkPreview(null);
    } catch (reason) {
      setBulkError(reason instanceof Error ? reason.message : "Toplu yayınlama başlatılamadı.");
    } finally {
      setBulkBusy(false);
    }
  }

  function resetForm() {
    setSelectedId(null);
    setBrandId(null);
    setCategoryIds([]);
    setPrimaryCategoryId(null);
    setTagIds([]);
    setAttributeValues({});
    setAttributeUnits({});
    setSku("");
    setIsPublished(false);
    setTr(emptyTranslation);
    setEn(emptyTranslation);
    setIncludeEnglish(false);
    setError(null);
    setMessage(null);
    setDirty(false);
  }

  async function selectProduct(productId: string) {
    setBusy(true);
    setError(null);
    try {
      const response = await fetch(
        `/api/admin/catalog/products/${productId}`,
        { cache: "no-store" }
      );
      if (!response.ok) throw new Error(await readError(response));
      const product = (await response.json()) as AdminProductDetail;
      const turkish = product.translations.find(
        (item) => item.languageCode === "tr"
      );
      const english = product.translations.find(
        (item) => item.languageCode === "en"
      );
      setSelectedId(product.id);
      setBrandId(product.brandId);
      setCategoryIds(product.categoryIds ?? []);
      setPrimaryCategoryId(product.primaryCategoryId);
      setTagIds(product.tagIds ?? []);
      const loadedValues: Record<string, string[]> = {};
      const loadedUnits: Record<string, string> = {};
      for (const value of product.attributeValues ?? []) {
        const rendered = value.textValue ??
          (value.numericValue === null ? null : String(value.numericValue)) ??
          (value.booleanValue === null ? null : String(value.booleanValue)) ??
          value.attributeOptionId;
        if (rendered !== null) {
          (loadedValues[value.attributeId] ??= []).push(rendered);
        }
        if (value.unitId) loadedUnits[value.attributeId] = value.unitId;
      }
      setAttributeValues(loadedValues);
      setAttributeUnits(loadedUnits);
      setSku(product.sku);
      setIsPublished(product.isPublished);
      setTr(toDraft(turkish));
      setEn(toDraft(english));
      setIncludeEnglish(Boolean(english));
      setMessage(null);
      setDirty(false);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Ürün alınamadı.");
    } finally {
      setBusy(false);
    }
  }

  async function searchProducts(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await loadProducts(search, 1);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Arama başarısız.");
    } finally {
      setBusy(false);
    }
  }

  function moveCategory(index: number, direction: -1 | 1) {
    const target = index + direction;
    if (target < 0 || target >= categoryIds.length) return;
    const next = [...categoryIds];
    [next[index], next[target]] = [next[target], next[index]];
    setCategoryIds(next);
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setMessage(null);
    const payload: SaveAdminProduct = {
      sku,
      brandId,
      categoryIds,
      primaryCategoryId,
      tagIds,
      attributeValues: buildAttributePayload(
        attributeCatalog,
        categoryIds,
        attributeValues,
        attributeUnits
      ),
      isPublished,
      translations: [
        toTranslation("tr", tr),
        ...(includeEnglish ? [toTranslation("en", en)] : [])
      ]
    };
    try {
      const response = await fetch(
        selectedId
          ? `/api/admin/catalog/products/${selectedId}`
          : "/api/admin/catalog/products",
        {
          method: selectedId ? "PUT" : "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        }
      );
      if (!response.ok) throw new Error(await readError(response));
      const saved = (await response.json()) as AdminProductDetail;
      setSelectedId(saved.id);
      setDirty(false);
      setMessage(selectedId ? "Ürün güncellendi." : "Ürün oluşturuldu.");
      await loadProducts(search, page);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Ürün kaydedilemedi.");
    } finally {
      setBusy(false);
    }
  }

  async function remove() {
    if (!selectedId || !window.confirm("Ürün yayından kaldırılıp arşivlensin mi?")) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const response = await fetch(
        `/api/admin/catalog/products/${selectedId}`,
        { method: "DELETE" }
      );
      if (!response.ok) throw new Error(await readError(response));
      resetForm();
      setMessage("Ürün arşivlendi.");
      await loadProducts(search, Math.max(1, page - (products.length === 1 ? 1 : 0)));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Ürün arşivlenemedi.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <div>
          <p className={styles.eyebrow}>EKİPHAN · ADMIN</p>
          <h1>Ürün yönetimi</h1>
          <p className={styles.lead}>
            Temel ürün kimliğini, yayın durumunu ve Türkçe/İngilizce metinleri yönetin.
          </p>
        </div>
        <nav className={styles.nav} aria-label="Admin menüsü">
          <a href="/admin">Dashboard</a>
          <a href="/admin/catalog/categories">Kategoriler</a>
          <a href="/admin/catalog/attributes">Özellikler</a>
          <a href="/admin/catalog/variants">Varyantlar</a>
          <a href="/admin/catalog/dictionaries">Etiketler ve birimler</a>
          <a href="/admin/media">Medya</a>
          <a href="/admin/catalog/brands">Markalar</a>
          <a href="/admin/catalog/relations">Ürün ilişkileri</a>
          <a href="/admin/imports">Ürün importu</a>
          <a href="/admin/quotes">Teklifler</a>
          <a href="/">Siteye dön</a>
        </nav>
      </header>
      {error && <div className={styles.error} role="alert">{error}</div>}
      {message && <div className={styles.success} role="status">{message}</div>}
      <div className={styles.layout}>
        <section className={styles.panel} aria-labelledby="list-title">
          <div className={styles.sectionHeader}>
            <h2 id="list-title">Ürünler</h2>
            <button type="button" onClick={resetForm}>Yeni ürün</button>
          </div>
          <form className={styles.search} onSubmit={searchProducts}>
            <label htmlFor="product-search">Ürün adı veya SKU</label>
            <div><input id="product-search" value={search} maxLength={100} onChange={(e) => setSearch(e.target.value)} /><button disabled={busy}>Ara</button></div>
            <div className={styles.filters}>
              <label>Marka<select value={brandFilter} onChange={(event) => setBrandFilter(event.target.value)}><option value="">Tüm markalar</option>{brands?.items.map((brand) => <option key={brand.id} value={brand.id}>{brand.name}</option>)}</select></label>
              <label>Kategori<select value={categoryFilter} onChange={(event) => setCategoryFilter(event.target.value)}><option value="">Tüm kategoriler</option>{structure?.categories.map((category) => <option key={category.id} value={category.id}>{category.translations.find((item) => item.languageCode === "tr")?.name ?? category.translations[0]?.name ?? "Adsız kategori"}</option>)}</select></label>
              <label>Yayın durumu<select value={publicationFilter} onChange={(event) => setPublicationFilter(event.target.value)}><option value="">Tümü</option><option value="true">Yayında</option><option value="false">Pasif</option></select></label>
              <label>Sırala<select value={sortBy} onChange={(event) => setSortBy(event.target.value)}><option value="updatedAt">Güncellenme</option><option value="name">Ürün adı</option><option value="sku">SKU</option><option value="createdAt">Oluşturulma</option></select></label>
              <label>Yön<select value={sortDirection} onChange={(event) => setSortDirection(event.target.value)}><option value="desc">Azalan</option><option value="asc">Artan</option></select></label>
              <label>Sayfa boyutu<select value={pageSize} onChange={(event) => setPageSize(Number(event.target.value))}><option value={20}>20</option><option value={50}>50</option><option value={100}>100</option></select></label>
            </div>
            <div className={styles.filterChecks}>
              <label className={styles.checkbox}><input type="checkbox" checked={missingImageFilter} onChange={(event) => setMissingImageFilter(event.target.checked)} />Eksik görsel</label>
              <label className={styles.checkbox}><input type="checkbox" checked={missingEnglishFilter} onChange={(event) => setMissingEnglishFilter(event.target.checked)} />Eksik İngilizce</label>
              <button type="submit" disabled={listLoading}>Filtreleri uygula</button>
            </div>
          </form>
          <section className={styles.bulkPublish} aria-labelledby="bulk-publish-title">
            <h3 id="bulk-publish-title">Toplu yayınlama</h3>
            <p>Yalnız pasif ve Marka veya Arama ile sınırlandırılmış ürünler için kullanılabilir.</p>
            <button
              type="button"
              disabled={!canBulkPublish || bulkBusy || Boolean(bulkOperation && !isBulkOperationComplete(bulkOperation.status))}
              onClick={() => void previewBulkPublish()}
            >
              {bulkBusy ? "Önizleme hazırlanıyor…" : "Filtrelenen pasif ürünleri toplu yayınla"}
            </button>
            {!canBulkPublish && <small>Önce Yayın durumu = Pasif seçin; ayrıca Marka veya Arama alanını doldurun.</small>}
            {bulkError && <p className={styles.bulkError} role="alert">{bulkError}</p>}
            {bulkPreview && <div className={styles.bulkResult}>
              <p>Hedef: <strong>{bulkTargetCount}</strong> · Uygun: <strong>{bulkPreview.eligibleProducts}</strong> · Atlanacak: <strong>{bulkPreview.skippedProducts}</strong> · Geçersiz: <strong>{bulkPreview.invalidProducts}</strong></p>
              <button type="button" disabled={bulkBusy || bulkPreview.eligibleProducts === 0} onClick={() => void executeBulkPublish()}>
                Onayla ve arka planda başlat
              </button>
              <button type="button" className={styles.secondaryButton} disabled={bulkBusy} onClick={() => setBulkPreview(null)}>Vazgeç</button>
            </div>}
            {bulkOperation && <div className={styles.bulkResult} role="status">
              <p>İşlem durumu: <strong>{bulkOperationStatusLabel(bulkOperation.status)}</strong> · %{bulkOperation.progressPercentage}</p>
              <p>Başarılı: {bulkOperation.successCount} · Başarısız: {bulkOperation.failedCount} · Atlanan: {bulkOperation.skippedCount} / {bulkOperation.totalCount}</p>
              {bulkOperation.errorMessage && <p className={styles.bulkError}>{bulkOperation.errorMessage}</p>}
              {bulkOperation.failedCount > 0 && <a href={`/api/admin/products/bulk-operations/${bulkOperation.operationId}/errors`}>Başarısız öğeler CSV raporunu indir</a>}
            </div>}
          </section>
          <div className={styles.productList}>
            {listLoading && <p className={styles.listState} role="status">Ürünler yükleniyor…</p>}
            {!listLoading && products.length === 0 && <p className={styles.listState}>Filtrelere uygun ürün bulunamadı.</p>}
            {products.map((product) => (
              <button type="button" key={product.id} disabled={busy} className={selectedId === product.id ? styles.selected : styles.product} onClick={() => void selectProduct(product.id)}>
                <strong>{product.name}</strong><span>{product.sku}</span><small>{product.isPublished ? "Yayında" : "Pasif"}{product.brandName ? ` · ${product.brandName}` : ""}{product.missingGalleryImage ? " · Görsel eksik" : ""}{product.missingEnglishContent ? " · EN eksik" : ""}</small>
              </button>
            ))}
          </div>
          <div className={styles.pagination}>
            <button type="button" disabled={listLoading || page <= 1} onClick={() => void loadProducts(search, page - 1)}>Önceki</button>
            <span>{totalCount === 0 ? "0 ürün" : `${page}. sayfa · ${totalCount} ürün`}</span>
            <button type="button" disabled={listLoading || page * pageSize >= totalCount} onClick={() => void loadProducts(search, page + 1)}>Sonraki</button>
          </div>
        </section>
        <section className={styles.panel} aria-labelledby="editor-title">
          <h2 id="editor-title">{selectedId ? "Ürünü düzenle" : "Yeni ürün"}</h2>
          {selectedId && <nav className={styles.productTools} aria-label="Ürün detay araçları">
            <a href="/admin/media">Görseller ve PDF katalog</a>
            <a href="/admin/catalog/variants">Varyant SKU’ları</a>
            <a href="/admin/catalog/relations">Benzer ve tamamlayıcı ürünler</a>
          </nav>}
          <form className={styles.editor} onSubmit={save} onChangeCapture={() => setDirty(true)}>
            <label>SKU<input required maxLength={100} value={sku} onChange={(e) => setSku(e.target.value)} /></label>
            <label>Marka<select value={brandId ?? ""} onChange={(e) => setBrandId(e.target.value || null)}><option value="">Markasız</option>{brands?.items.map((brand) => <option key={brand.id} value={brand.id}>{brand.name}</option>)}</select></label>
            <label>Kategoriler<select multiple size={6} value={categoryIds}
              onChange={(event) => {
                const values = Array.from(event.target.selectedOptions, (item) => item.value);
                setCategoryIds(values);
                if (primaryCategoryId && !values.includes(primaryCategoryId)) {
                  setPrimaryCategoryId(null);
                }
              }}>
              {structure?.categories.map((category) => {
                const name = category.translations.find((item) => item.languageCode === "tr")?.name ?? category.translations[0]?.name ?? "Adsız kategori";
                return <option key={category.id} value={category.id}>{name}</option>;
              })}
            </select><small>Birden fazla seçim için Ctrl/Command tuşunu kullanın.</small></label>
            {categoryIds.length > 0 && <ol className={styles.categoryOrder}>
              {categoryIds.map((categoryId, index) => {
                const category = structure?.categories.find((item) => item.id === categoryId);
                const name = category?.translations.find((item) => item.languageCode === "tr")?.name ?? category?.translations[0]?.name ?? categoryId;
                return <li key={categoryId}><span>{name}</span><div>
                  <button type="button" disabled={index === 0} aria-label={`${name} kategorisini yukarı taşı`} onClick={() => moveCategory(index, -1)}>↑</button>
                  <button type="button" disabled={index === categoryIds.length - 1} aria-label={`${name} kategorisini aşağı taşı`} onClick={() => moveCategory(index, 1)}>↓</button>
                </div></li>;
              })}
            </ol>}
            <label>Ana kategori<select value={primaryCategoryId ?? ""}
              onChange={(event) => setPrimaryCategoryId(event.target.value || null)}>
              <option value="">Ana kategori yok</option>
              {structure?.categories.filter((category) => categoryIds.includes(category.id)).map((category) => {
                const name = category.translations.find((item) => item.languageCode === "tr")?.name ?? category.translations[0]?.name ?? "Adsız kategori";
                return <option key={category.id} value={category.id}>{name}</option>;
              })}
            </select></label>
            <label>Etiketler<select multiple size={5} value={tagIds}
              onChange={(event) => setTagIds(Array.from(
                event.target.selectedOptions,
                (item) => item.value))}>
              {dictionary?.tags.filter((tag) => tag.isActive || tagIds.includes(tag.id)).map((tag) => (
                <option key={tag.id} value={tag.id}>
                  {tag.translations.find((item) => item.languageCode === "tr")?.name ?? tag.code}
                </option>
              ))}
            </select><small>Ürünü filtrelemek ve gruplamak için çoklu seçim yapın.</small></label>
            <DynamicAttributeFields
              catalog={attributeCatalog}
              categoryIds={categoryIds}
              values={attributeValues}
              units={attributeUnits}
              setValues={setAttributeValues}
              setUnits={setAttributeUnits}
            />
            <label className={styles.checkbox}><input type="checkbox" checked={isPublished} onChange={(e) => setIsPublished(e.target.checked)} />Yayında</label>
            <TranslationFields title="Türkçe" prefix="tr" value={tr} setValue={setTr} required />
            <label className={styles.checkbox}><input type="checkbox" checked={includeEnglish} onChange={(e) => setIncludeEnglish(e.target.checked)} />İngilizce çeviriyi yönet</label>
            {includeEnglish && <TranslationFields title="İngilizce" prefix="en" value={en} setValue={setEn} required />}
            <div className={styles.actions}>
              <button type="submit" disabled={busy}>{busy ? "Kaydediliyor…" : "Kaydet"}</button>
              {selectedId && <button className={styles.danger} type="button" disabled={busy} onClick={() => void remove()}>Arşivle</button>}
              {dirty && <span className={styles.unsaved} role="status">Kaydedilmemiş değişiklikler var.</span>}
            </div>
          </form>
        </section>
      </div>
    </div>
  );
}

function isBulkOperationComplete(status: number): boolean {
  return status === 3 || status === 4 || status === 5 || status === 6;
}

function bulkOperationStatusLabel(status: number): string {
  return ({ 1: "Bekliyor", 2: "İşleniyor", 3: "Tamamlandı", 4: "Kısmen tamamlandı", 5: "Başarısız", 6: "İptal edildi" } as Record<number, string>)[status] ?? "Bilinmiyor";
}

function TranslationFields({ title, prefix, value, setValue, required }: { title: string; prefix: string; value: TranslationDraft; setValue: (value: TranslationDraft) => void; required: boolean }) {
  const field = (
    key: Exclude<keyof TranslationDraft, "noIndex">,
    next: string
  ) =>
    setValue({ ...value, [key]: next });
  return <fieldset><legend>{title}</legend>
    <label htmlFor={`${prefix}-name`}>Ürün adı<input id={`${prefix}-name`} required={required} maxLength={250} value={value.name} onChange={(e) => field("name", e.target.value)} /></label>
    <label htmlFor={`${prefix}-slug`}>URL slug<input id={`${prefix}-slug`} required={required} maxLength={300} value={value.slug} onChange={(e) => field("slug", e.target.value)} /></label>
    <label htmlFor={`${prefix}-short`}>Kısa açıklama<textarea id={`${prefix}-short`} maxLength={500} value={value.shortDescription} onChange={(e) => field("shortDescription", e.target.value)} /></label>
    <label htmlFor={`${prefix}-long`}>Uzun açıklama<textarea id={`${prefix}-long`} value={value.longDescription} onChange={(e) => field("longDescription", e.target.value)} /></label>
    <label htmlFor={`${prefix}-meta-title`}>Meta title<input id={`${prefix}-meta-title`} maxLength={70} value={value.metaTitle} onChange={(e) => field("metaTitle", e.target.value)} /><small>{value.metaTitle.length}/70</small></label>
    <label htmlFor={`${prefix}-meta-description`}>Meta description<textarea id={`${prefix}-meta-description`} maxLength={320} value={value.metaDescription} onChange={(e) => field("metaDescription", e.target.value)} /><small>{value.metaDescription.length}/320</small></label>
    <label htmlFor={`${prefix}-canonical`}>Canonical URL<input id={`${prefix}-canonical`} type="url" maxLength={2048} placeholder="https://www.ekiphan.com/..." value={value.canonicalUrl} onChange={(e) => field("canonicalUrl", e.target.value)} /></label>
    <label className={styles.checkbox}><input type="checkbox" checked={value.noIndex} onChange={(e) => setValue({ ...value, noIndex: e.target.checked })} />İndekslemeyi engelle (noindex)</label>
  </fieldset>;
}

function toDraft(value?: AdminProductTranslation): TranslationDraft {
  return value ? { name: value.name, slug: value.slug, shortDescription: value.shortDescription ?? "", longDescription: value.longDescription ?? "", metaTitle: value.metaTitle ?? "", metaDescription: value.metaDescription ?? "", canonicalUrl: value.canonicalUrl ?? "", noIndex: value.noIndex ?? false, noFollow: value.noFollow ?? false, openGraphTitle: value.openGraphTitle ?? "", openGraphDescription: value.openGraphDescription ?? "", openGraphImageMediaId: value.openGraphImageMediaId ?? null } : emptyTranslation;
}

function toTranslation(languageCode: "tr" | "en", value: TranslationDraft): AdminProductTranslation {
  return { languageCode, name: value.name, slug: value.slug, shortDescription: value.shortDescription || null, longDescription: value.longDescription || null, metaTitle: value.metaTitle || null, metaDescription: value.metaDescription || null, canonicalUrl: value.canonicalUrl || null, noIndex: value.noIndex, noFollow: value.noFollow, openGraphTitle: value.openGraphTitle || null, openGraphDescription: value.openGraphDescription || null, openGraphImageMediaId: value.openGraphImageMediaId };
}

function DynamicAttributeFields({
  catalog,
  categoryIds,
  values,
  units,
  setValues,
  setUnits
}: {
  catalog: AdminAttributeCatalog | null;
  categoryIds: string[];
  values: Record<string, string[]>;
  units: Record<string, string>;
  setValues: (value: Record<string, string[]>) => void;
  setUnits: (value: Record<string, string>) => void;
}) {
  if (!catalog || categoryIds.length === 0) return null;
  const assignments = catalog.assignments.filter((item) =>
    categoryIds.includes(item.categoryId));
  const ids = new Set(assignments.map((item) => item.attributeId));
  const attributes = catalog.attributes.filter((item) =>
    item.isActive && ids.has(item.id));
  if (attributes.length === 0) return null;
  const update = (id: string, next: string[]) =>
    setValues({ ...values, [id]: next });

  return <fieldset><legend>Teknik özellikler</legend>
    {attributes.map((attribute) => {
      const required = assignments.some((item) =>
        item.attributeId === attribute.id && item.isRequired);
      const name = attribute.translations.find((item) =>
        item.languageCode === "tr")?.name ?? attribute.code;
      const current = values[attribute.id] ?? [];
      if (attribute.dataType === "Boolean") {
        return <label key={attribute.id}>{name}<select required={required}
          value={current[0] ?? ""} onChange={(e) => update(attribute.id, e.target.value ? [e.target.value] : [])}>
          <option value="">Seçin</option><option value="true">Evet</option><option value="false">Hayır</option>
        </select></label>;
      }
      if (attribute.dataType === "Option" || attribute.dataType === "MultiOption") {
        const multiple = attribute.dataType === "MultiOption";
        return <label key={attribute.id}>{name}<select required={required}
          multiple={multiple} size={multiple ? Math.min(6, Math.max(2, attribute.options.length)) : undefined}
          value={multiple ? current : current[0] ?? ""}
          onChange={(e) => update(attribute.id, multiple
            ? Array.from(e.target.selectedOptions, (item) => item.value)
            : e.target.value ? [e.target.value] : [])}>
          {!multiple && <option value="">Seçin</option>}
          {attribute.options.filter((item) => item.isActive).map((option) =>
            <option key={option.id} value={option.id}>
              {option.translations.find((item) => item.languageCode === "tr")?.name ?? option.code}
            </option>)}
        </select></label>;
      }
      if (attribute.dataType === "Number") {
        const availableUnits = catalog.units.filter((item) =>
          item.isActive && item.dimension === attribute.unitDimension);
        return <div key={attribute.id}>
          <label>{name}<input type="number" step="any" required={required}
            value={current[0] ?? ""} onChange={(e) => update(attribute.id, e.target.value ? [e.target.value] : [])} /></label>
          {attribute.unitDimension && <label>Birim<select required={required}
            value={units[attribute.id] ?? ""}
            onChange={(e) => setUnits({ ...units, [attribute.id]: e.target.value })}>
            <option value="">Birim seçin</option>
            {availableUnits.map((unit) => <option key={unit.id} value={unit.id}>{unit.symbol}</option>)}
          </select></label>}
        </div>;
      }
      return <label key={attribute.id}>{name}<input required={required}
        maxLength={2000} value={current[0] ?? ""}
        onChange={(e) => update(attribute.id, e.target.value ? [e.target.value] : [])} /></label>;
    })}
  </fieldset>;
}

function buildAttributePayload(
  catalog: AdminAttributeCatalog | null,
  categoryIds: string[],
  values: Record<string, string[]>,
  units: Record<string, string>
): AdminProductAttributeValue[] {
  if (!catalog) return [];
  const allowed = new Set(catalog.assignments
    .filter((item) => categoryIds.includes(item.categoryId))
    .map((item) => item.attributeId));
  return catalog.attributes
    .filter((attribute) => allowed.has(attribute.id))
    .flatMap((attribute) => (values[attribute.id] ?? []).map((value, sequence) => ({
      attributeId: attribute.id,
      sequence,
      textValue: attribute.dataType === "Text" ? value : null,
      numericValue: attribute.dataType === "Number" ? Number(value) : null,
      booleanValue: attribute.dataType === "Boolean" ? value === "true" : null,
      attributeOptionId:
        attribute.dataType === "Option" || attribute.dataType === "MultiOption"
          ? value
          : null,
      unitId: attribute.dataType === "Number" ? units[attribute.id] || null : null
    })));
}
