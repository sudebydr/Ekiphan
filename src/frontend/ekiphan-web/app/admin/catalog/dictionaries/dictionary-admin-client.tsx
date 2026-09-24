"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import type {
  AdminDictionaryCatalog,
  AdminTag,
  AdminUnitDefinition
} from "../../../../lib/admin-dictionary-types";
import type {
  AdminCatalogProduct,
  AdminCatalogProductPage,
  ProblemDetails
} from "../../../../lib/admin-product-relation-types";
import styles from "../products/products.module.css";

async function readError(response: Response) {
  try {
    const problem = (await response.json()) as ProblemDetails;
    return problem.detail ?? problem.title ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function DictionaryAdminClient() {
  const [catalog, setCatalog] = useState<AdminDictionaryCatalog>({ tags: [], units: [], productTags: [] });
  const [products, setProducts] = useState<AdminCatalogProduct[]>([]);
  const [tagId, setTagId] = useState<string | null>(null);
  const [tagCode, setTagCode] = useState("");
  const [tagActive, setTagActive] = useState(true);
  const [tagTrName, setTagTrName] = useState("");
  const [tagTrSlug, setTagTrSlug] = useState("");
  const [tagEnName, setTagEnName] = useState("");
  const [tagEnSlug, setTagEnSlug] = useState("");
  const [unitId, setUnitId] = useState<string | null>(null);
  const [unitCode, setUnitCode] = useState("");
  const [symbol, setSymbol] = useState("");
  const [dimension, setDimension] = useState("");
  const [factor, setFactor] = useState(1);
  const [baseUnit, setBaseUnit] = useState(false);
  const [unitActive, setUnitActive] = useState(true);
  const [productId, setProductId] = useState("");
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    const response = await fetch("/api/admin/catalog/dictionaries", { cache: "no-store" });
    if (!response.ok) throw new Error(await readError(response));
    setCatalog((await response.json()) as AdminDictionaryCatalog);
  }, []);

  useEffect(() => {
    void Promise.all([
      load(),
      fetch("/api/admin/catalog/products?page=1&pageSize=100&language=tr", { cache: "no-store" })
        .then(async (response) => {
          if (!response.ok) throw new Error(await readError(response));
          setProducts(((await response.json()) as AdminCatalogProductPage).items);
        })
    ]).catch((reason: unknown) =>
      setError(reason instanceof Error ? reason.message : "Sözlükler alınamadı."));
  }, [load]);

  const nameOf = (tag: AdminTag) =>
    tag.translations.find((item) => item.languageCode === "tr")?.name ??
    tag.translations[0]?.name ?? tag.code;

  function resetTag() {
    setTagId(null); setTagCode(""); setTagActive(true);
    setTagTrName(""); setTagTrSlug(""); setTagEnName(""); setTagEnSlug("");
  }

  function chooseTag(tag: AdminTag) {
    setTagId(tag.id); setTagCode(tag.code); setTagActive(tag.isActive);
    const tr = tag.translations.find((item) => item.languageCode === "tr");
    const en = tag.translations.find((item) => item.languageCode === "en");
    setTagTrName(tr?.name ?? ""); setTagTrSlug(tr?.slug ?? "");
    setTagEnName(en?.name ?? ""); setTagEnSlug(en?.slug ?? "");
  }

  function resetUnit() {
    setUnitId(null); setUnitCode(""); setSymbol(""); setDimension("");
    setFactor(1); setBaseUnit(false); setUnitActive(true);
  }

  function chooseUnit(unit: AdminUnitDefinition) {
    setUnitId(unit.id); setUnitCode(unit.code); setSymbol(unit.symbol);
    setDimension(unit.dimension); setFactor(unit.conversionFactorToBase);
    setBaseUnit(unit.isBaseUnit); setUnitActive(unit.isActive);
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
      await load();
      return true;
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Kayıt tamamlanamadı.");
      return false;
    } finally { setBusy(false); }
  }

  async function saveTag(event: FormEvent) {
    event.preventDefault();
    const ok = await request(
      tagId ? `/api/admin/catalog/tags/${tagId}` : "/api/admin/catalog/tags",
      tagId ? "PUT" : "POST",
      {
        code: tagCode, isActive: tagActive,
        translations: [
          { languageCode: "tr", name: tagTrName, slug: tagTrSlug },
          ...(tagEnName ? [{ languageCode: "en", name: tagEnName, slug: tagEnSlug }] : [])
        ]
      });
    if (ok) setMessage(tagId ? "Etiket güncellendi." : "Etiket oluşturuldu.");
  }

  async function saveUnit(event: FormEvent) {
    event.preventDefault();
    const ok = await request(
      unitId ? `/api/admin/catalog/units/${unitId}` : "/api/admin/catalog/units",
      unitId ? "PUT" : "POST",
      {
        code: unitCode, symbol, dimension,
        conversionFactorToBase: factor,
        isBaseUnit: baseUnit,
        isActive: unitActive
      });
    if (ok) setMessage(unitId ? "Birim güncellendi." : "Birim oluşturuldu.");
  }

  async function saveProductTags(event: FormEvent) {
    event.preventDefault();
    const ok = await request(
      `/api/admin/catalog/products/${productId}/tags`,
      "PUT",
      { tagIds: selectedTags });
    if (ok) setMessage("Ürün etiketleri güncellendi.");
  }

  return <main className={styles.page}>
    <header className={styles.header}><div><p className={styles.eyebrow}>EKİPHAN · ADMIN</p><h1>Etiket ve birimler</h1>
      <p className={styles.lead}>Kontrollü ürün etiketlerini ve teknik ölçü dönüşüm sözlüğünü yönetin.</p></div>
      <nav className={styles.nav}><a href="/admin/catalog/products">Ürünler</a><a href="/admin/catalog/attributes">Özellikler</a><a href="/admin/media">Medya</a><a href="/">Siteye dön</a></nav>
    </header>
    {error && <div className={styles.error} role="alert">{error}</div>}
    {message && <div className={styles.success} role="status">{message}</div>}
    <div className={styles.layout}>
      <section className={styles.panel}>
        <div className={styles.sectionHeader}><h2>Etiketler</h2><button type="button" onClick={resetTag}>Yeni etiket</button></div>
        <div className={styles.productList}>{catalog.tags.map((tag) =>
          <button type="button" key={tag.id} onClick={() => chooseTag(tag)}
            className={tagId === tag.id ? styles.selected : styles.product}>
            <strong>{nameOf(tag)}</strong><span>{tag.code}</span><small>{tag.isActive ? "Aktif" : "Pasif"}</small>
          </button>)}</div>
        <form className={styles.editor} onSubmit={saveTag}>
          <label>Kod<input required maxLength={100} value={tagCode} onChange={(e) => setTagCode(e.target.value)} /></label>
          <label>Türkçe ad<input required maxLength={150} value={tagTrName} onChange={(e) => setTagTrName(e.target.value)} /></label>
          <label>Türkçe slug<input required maxLength={200} value={tagTrSlug} onChange={(e) => setTagTrSlug(e.target.value)} /></label>
          <label>İngilizce ad<input maxLength={150} value={tagEnName} onChange={(e) => setTagEnName(e.target.value)} /></label>
          {tagEnName && <label>İngilizce slug<input required maxLength={200} value={tagEnSlug} onChange={(e) => setTagEnSlug(e.target.value)} /></label>}
          <label className={styles.checkbox}><input type="checkbox" checked={tagActive} onChange={(e) => setTagActive(e.target.checked)} />Aktif</label>
          <button disabled={busy}>Etiketi kaydet</button>
        </form>
      </section>
      <section className={styles.panel}>
        <div className={styles.sectionHeader}><h2>Ölçü birimleri</h2><button type="button" onClick={resetUnit}>Yeni birim</button></div>
        <div className={styles.productList}>{catalog.units.map((unit) =>
          <button type="button" key={unit.id} onClick={() => chooseUnit(unit)}
            className={unitId === unit.id ? styles.selected : styles.product}>
            <strong>{unit.code} · {unit.symbol}</strong><span>{unit.dimension}</span><small>{unit.isBaseUnit ? "Temel birim" : `× ${unit.conversionFactorToBase}`}</small>
          </button>)}</div>
        <form className={styles.editor} onSubmit={saveUnit}>
          <label>Kod<input required maxLength={50} value={unitCode} onChange={(e) => setUnitCode(e.target.value)} /></label>
          <label>Sembol<input required maxLength={20} value={symbol} onChange={(e) => setSymbol(e.target.value)} /></label>
          <label>Boyut<input required disabled={Boolean(unitId)} maxLength={50} value={dimension} onChange={(e) => setDimension(e.target.value)} /></label>
          <label>Temel birime dönüşüm katsayısı<input type="number" step="any" min="0.0000000001" required value={factor} onChange={(e) => setFactor(e.target.valueAsNumber)} /></label>
          <label className={styles.checkbox}><input type="checkbox" disabled={Boolean(unitId)} checked={baseUnit} onChange={(e) => { setBaseUnit(e.target.checked); if (e.target.checked) setFactor(1); }} />Temel birim</label>
          <label className={styles.checkbox}><input type="checkbox" checked={unitActive} onChange={(e) => setUnitActive(e.target.checked)} />Aktif</label>
          <button disabled={busy}>Birimi kaydet</button>
        </form>
      </section>
    </div>
    <section className={styles.panel}><h2>Ürün etiketi atama</h2>
      <form className={styles.editor} onSubmit={saveProductTags}>
        <label>Ürün<select required value={productId} onChange={(e) => {
          const id = e.target.value; setProductId(id);
          setSelectedTags(catalog.productTags.find((item) => item.productId === id)?.tagIds ?? []);
        }}><option value="">Seçin</option>{products.map((item) =>
          <option key={item.id} value={item.id}>{item.name} · {item.sku}</option>)}</select></label>
        <label>Etiketler<select multiple size={Math.min(8, Math.max(3, catalog.tags.length))}
          value={selectedTags} onChange={(e) => setSelectedTags(Array.from(e.target.selectedOptions, (item) => item.value))}>
          {catalog.tags.filter((item) => item.isActive || selectedTags.includes(item.id)).map((tag) =>
            <option key={tag.id} value={tag.id}>{nameOf(tag)}</option>)}
        </select></label>
        <button disabled={busy || !productId}>Atamaları kaydet</button>
      </form>
    </section>
  </main>;
}
