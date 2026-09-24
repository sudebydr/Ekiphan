"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import type {
  AdminProductVariant,
  AdminProductVariantCatalog,
  AdminVariantGroup,
  AdminVariantOption
} from "../../../../lib/admin-variant-types";
import type {
  AdminCatalogProduct,
  AdminCatalogProductPage,
  ProblemDetails
} from "../../../../lib/admin-product-relation-types";
import { AdminMediaPicker } from "../../../../components/admin-media-picker";
import { useAdminSession } from "../../admin-session-guard";
import styles from "../products/products.module.css";

async function readError(response: Response) {
  try {
    const problem = (await response.json()) as ProblemDetails;
    return problem.detail ?? problem.title ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function VariantAdminClient() {
  const { hasPermission } = useAdminSession();
  const [products, setProducts] = useState<AdminCatalogProduct[]>([]);
  const [product, setProduct] = useState<AdminCatalogProduct | null>(null);
  const [catalog, setCatalog] = useState<AdminProductVariantCatalog | null>(null);
  const [groupId, setGroupId] = useState<string | null>(null);
  const [groupCode, setGroupCode] = useState("");
  const [groupSort, setGroupSort] = useState(0);
  const [groupTr, setGroupTr] = useState("");
  const [groupEn, setGroupEn] = useState("");
  const [optionId, setOptionId] = useState<string | null>(null);
  const [optionCode, setOptionCode] = useState("");
  const [optionSort, setOptionSort] = useState(0);
  const [optionActive, setOptionActive] = useState(true);
  const [optionTr, setOptionTr] = useState("");
  const [optionEn, setOptionEn] = useState("");
  const [variantId, setVariantId] = useState<string | null>(null);
  const [variantSKU, setVariantSKU] = useState("");
  const [variantSort, setVariantSort] = useState(0);
  const [variantActive, setVariantActive] = useState(true);
  const [variantMediaAssetId, setVariantMediaAssetId] = useState("");
  const [selections, setSelections] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    void fetch("/api/admin/catalog/products?page=1&pageSize=100&language=tr", {
      cache: "no-store"
    }).then(async (response) => {
      if (!response.ok) throw new Error(await readError(response));
      setProducts(((await response.json()) as AdminCatalogProductPage).items);
    }).catch((reason: unknown) =>
      setError(reason instanceof Error ? reason.message : "Ürünler alınamadı."));
  }, []);

  const load = useCallback(async (productId: string) => {
    const response = await fetch(
      `/api/admin/catalog/products/${productId}/variants`,
      { cache: "no-store" });
    if (!response.ok) throw new Error(await readError(response));
    setCatalog((await response.json()) as AdminProductVariantCatalog);
  }, []);

  async function selectProduct(item: AdminCatalogProduct) {
    setProduct(item); setBusy(true); setError(null);
    resetGroup(); resetVariant();
    try { await load(item.id); }
    catch (reason) {
      setError(reason instanceof Error ? reason.message : "Varyantlar alınamadı.");
    } finally { setBusy(false); }
  }

  const nameOf = (item: { translations: { languageCode: string; name: string }[] }) =>
    item.translations.find((value) => value.languageCode === "tr")?.name ??
    item.translations[0]?.name ?? "Adsız";

  function resetGroup() {
    setGroupId(null); setGroupCode(""); setGroupSort(0);
    setGroupTr(""); setGroupEn(""); resetOption();
  }

  function chooseGroup(group: AdminVariantGroup) {
    setGroupId(group.id); setGroupCode(group.code); setGroupSort(group.sortOrder);
    setGroupTr(group.translations.find((item) => item.languageCode === "tr")?.name ?? "");
    setGroupEn(group.translations.find((item) => item.languageCode === "en")?.name ?? "");
    resetOption();
  }

  function resetOption() {
    setOptionId(null); setOptionCode(""); setOptionSort(0);
    setOptionActive(true); setOptionTr(""); setOptionEn("");
  }

  function chooseOption(option: AdminVariantOption) {
    setOptionId(option.id); setOptionCode(option.code); setOptionSort(option.sortOrder);
    setOptionActive(option.isActive);
    setOptionTr(option.translations.find((item) => item.languageCode === "tr")?.name ?? "");
    setOptionEn(option.translations.find((item) => item.languageCode === "en")?.name ?? "");
  }

  function resetVariant() {
    setVariantId(null); setVariantSKU(""); setVariantSort(0);
    setVariantActive(true); setSelections({});
    setVariantMediaAssetId("");
  }

  function chooseVariant(variant: AdminProductVariant) {
    setVariantId(variant.id); setVariantSKU(variant.sku);
    setVariantSort(variant.sortOrder); setVariantActive(variant.isActive);
    setSelections(variant.selectedOptions);
    setVariantMediaAssetId(variant.mediaAssetId ?? "");
  }

  async function request(url: string, method: "POST" | "PUT", body: object) {
    setBusy(true); setError(null); setMessage(null);
    try {
      const response = await fetch(url, {
        method,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
      });
      if (!response.ok) throw new Error(await readError(response));
      if (product) await load(product.id);
      return true;
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Kayıt tamamlanamadı.");
      return false;
    } finally { setBusy(false); }
  }

  const translations = (tr: string, en: string) => [
    { languageCode: "tr", name: tr },
    ...(en ? [{ languageCode: "en", name: en }] : [])
  ];

  async function saveGroup(event: FormEvent) {
    event.preventDefault(); if (!product) return;
    const ok = await request(
      groupId
        ? `/api/admin/catalog/products/${product.id}/variant-groups/${groupId}`
        : `/api/admin/catalog/products/${product.id}/variant-groups`,
      groupId ? "PUT" : "POST",
      { code: groupCode, sortOrder: groupSort, translations: translations(groupTr, groupEn) });
    if (ok) setMessage(groupId ? "Varyant grubu güncellendi." : "Varyant grubu oluşturuldu.");
  }

  async function saveOption(event: FormEvent) {
    event.preventDefault(); if (!product || !groupId) return;
    const ok = await request(
      optionId
        ? `/api/admin/catalog/products/${product.id}/variant-options/${optionId}`
        : `/api/admin/catalog/products/${product.id}/variant-groups/${groupId}/options`,
      optionId ? "PUT" : "POST",
      { code: optionCode, sortOrder: optionSort, isActive: optionActive, translations: translations(optionTr, optionEn) });
    if (ok) { setMessage(optionId ? "Seçenek güncellendi." : "Seçenek oluşturuldu."); resetOption(); }
  }

  async function saveVariant(event: FormEvent) {
    event.preventDefault(); if (!product) return;
    const ok = await request(
      variantId
        ? `/api/admin/catalog/products/${product.id}/variants/${variantId}`
        : `/api/admin/catalog/products/${product.id}/variants`,
      variantId ? "PUT" : "POST",
      { sku: variantSKU, sortOrder: variantSort, isActive: variantActive, selectedOptions: selections, mediaAssetId: variantMediaAssetId || null });
    if (ok) { setMessage(variantId ? "Varyant güncellendi." : "Varyant oluşturuldu."); resetVariant(); }
  }

  return <main className={styles.page}>
    <header className={styles.header}><div><p className={styles.eyebrow}>EKİPHAN · ADMIN</p><h1>Varyant yönetimi</h1>
      <p className={styles.lead}>En fazla iki seçenek ekseni oluşturun ve her kombinasyona benzersiz SKU atayın.</p></div>
      <nav className={styles.nav}><a href="/admin/catalog/products">Ürünler</a><a href="/admin/catalog/categories">Kategoriler</a><a href="/admin/catalog/attributes">Özellikler</a><a href="/">Siteye dön</a></nav>
    </header>
    {error && <div className={styles.error} role="alert">{error}</div>}
    {message && <div className={styles.success} role="status">{message}</div>}
    <section className={styles.panel}><h2>Ürün seçin</h2>
      <div className={styles.productList}>{products.map((item) =>
        <button type="button" key={item.id} onClick={() => void selectProduct(item)}
          className={product?.id === item.id ? styles.selected : styles.product}>
          <strong>{item.name}</strong><span>{item.sku}</span>
        </button>)}</div>
    </section>
    {product && <div className={styles.layout}>
      <section className={styles.panel}>
        <div className={styles.sectionHeader}><h2>Gruplar</h2><button type="button" onClick={resetGroup} disabled={(catalog?.groups.length ?? 0) >= 2}>Yeni grup</button></div>
        <div className={styles.productList}>{catalog?.groups.map((group) =>
          <button type="button" key={group.id} onClick={() => chooseGroup(group)}
            className={groupId === group.id ? styles.selected : styles.product}>
            <strong>{nameOf(group)}</strong><span>{group.code}</span>
          </button>)}</div>
        <form className={styles.editor} onSubmit={saveGroup}>
          <label>Grup kodu<input required maxLength={100} value={groupCode} onChange={(e) => setGroupCode(e.target.value)} /></label>
          <label>Türkçe ad<input required maxLength={150} value={groupTr} onChange={(e) => setGroupTr(e.target.value)} /></label>
          <label>İngilizce ad<input maxLength={150} value={groupEn} onChange={(e) => setGroupEn(e.target.value)} /></label>
          <label>Sıra<input type="number" value={groupSort} onChange={(e) => setGroupSort(e.target.valueAsNumber)} /></label>
          <button disabled={busy || (!groupId && (catalog?.groups.length ?? 0) >= 2)}>Grubu kaydet</button>
        </form>
        {groupId && <form className={styles.editor} onSubmit={saveOption}><h3>Grup seçenekleri</h3>
          <div className={styles.productList}>{catalog?.groups.find((item) => item.id === groupId)?.options.map((option) =>
            <button type="button" key={option.id} onClick={() => chooseOption(option)}
              className={optionId === option.id ? styles.selected : styles.product}>
              <strong>{nameOf(option)}</strong><span>{option.code}</span><small>{option.isActive ? "Aktif" : "Pasif"}</small>
            </button>)}</div>
          <button type="button" onClick={resetOption}>Yeni seçenek</button>
          <label>Kod<input required maxLength={100} value={optionCode} onChange={(e) => setOptionCode(e.target.value)} /></label>
          <label>Türkçe ad<input required maxLength={150} value={optionTr} onChange={(e) => setOptionTr(e.target.value)} /></label>
          <label>İngilizce ad<input maxLength={150} value={optionEn} onChange={(e) => setOptionEn(e.target.value)} /></label>
          <label>Sıra<input type="number" value={optionSort} onChange={(e) => setOptionSort(e.target.valueAsNumber)} /></label>
          <label className={styles.checkbox}><input type="checkbox" checked={optionActive} onChange={(e) => setOptionActive(e.target.checked)} />Aktif</label>
          <button disabled={busy}>Seçeneği kaydet</button>
        </form>}
      </section>
      <section className={styles.panel}>
        <div className={styles.sectionHeader}><h2>Varyant SKU’ları</h2><button type="button" onClick={resetVariant}>Yeni varyant</button></div>
        <div className={styles.productList}>{catalog?.variants.map((variant) =>
          <button type="button" key={variant.id} onClick={() => chooseVariant(variant)}
            className={variantId === variant.id ? styles.selected : styles.product}>
            <strong>{variant.sku}</strong><small>{variant.isActive ? "Aktif" : "Pasif"}</small>
          </button>)}</div>
        <form className={styles.editor} onSubmit={saveVariant}>
          <label>Varyant SKU<input required maxLength={100} value={variantSKU} onChange={(e) => setVariantSKU(e.target.value)} /></label>
          {catalog?.groups.map((group) => <label key={group.id}>{nameOf(group)}
            <select required value={selections[group.id] ?? ""} onChange={(e) => setSelections({ ...selections, [group.id]: e.target.value })}>
              <option value="">Seçin</option>{group.options.filter((item) => item.isActive || item.id === selections[group.id]).map((option) =>
                <option key={option.id} value={option.id}>{nameOf(option)}</option>)}
            </select></label>)}
          <label>Sıra<input type="number" value={variantSort} onChange={(e) => setVariantSort(e.target.valueAsNumber)} /></label>
          <label className={styles.checkbox}><input type="checkbox" checked={variantActive} onChange={(e) => setVariantActive(e.target.checked)} />Aktif</label>
          {hasPermission("media.manage") ? (
            <AdminMediaPicker
              label="Varyant görseli"
              value={variantMediaAssetId}
              onChange={setVariantMediaAssetId}
              acceptedTypes={["Image"]}
            />
          ) : (
            <p className={styles.listState}>Varyant görseli seçmek için medya yönetimi yetkisi gerekir.</p>
          )}
          <button disabled={busy || !catalog?.groups.length}>Varyantı kaydet</button>
        </form>
      </section>
    </div>}
  </main>;
}
