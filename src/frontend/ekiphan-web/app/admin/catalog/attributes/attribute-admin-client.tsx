"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import type {
  AdminAttribute,
  AdminAttributeCatalog,
  AdminAttributeOption
} from "../../../../lib/admin-attribute-types";
import type { AdminCatalogStructure } from "../../../../lib/admin-category-types";
import type { ProblemDetails } from "../../../../lib/admin-product-relation-types";
import styles from "../products/products.module.css";

type Translation = { name: string };
const emptyText: Translation = { name: "" };

async function errorText(response: Response) {
  try {
    const problem = (await response.json()) as ProblemDetails;
    return problem.detail ?? problem.title ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function AttributeAdminClient() {
  const [catalog, setCatalog] = useState<AdminAttributeCatalog>({ attributes: [], assignments: [], units: [] });
  const [structure, setStructure] = useState<AdminCatalogStructure>({ sections: [], categories: [] });
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [code, setCode] = useState("");
  const [dataType, setDataType] = useState<AdminAttribute["dataType"]>("Text");
  const [unitDimension, setUnitDimension] = useState("");
  const [active, setActive] = useState(true);
  const [tr, setTr] = useState<Translation>(emptyText);
  const [en, setEn] = useState<Translation>(emptyText);
  const [english, setEnglish] = useState(false);
  const [optionId, setOptionId] = useState<string | null>(null);
  const [optionCode, setOptionCode] = useState("");
  const [optionSort, setOptionSort] = useState(0);
  const [optionActive, setOptionActive] = useState(true);
  const [optionTr, setOptionTr] = useState("");
  const [optionEn, setOptionEn] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [assignmentAttributeId, setAssignmentAttributeId] = useState("");
  const [required, setRequired] = useState(false);
  const [filterable, setFilterable] = useState(false);
  const [visible, setVisible] = useState(true);
  const [comparison, setComparison] = useState(false);
  const [sortOrder, setSortOrder] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    const [attributeResponse, structureResponse] = await Promise.all([
      fetch("/api/admin/catalog/attributes", { cache: "no-store" }),
      fetch("/api/admin/catalog/structure", { cache: "no-store" })
    ]);
    if (!attributeResponse.ok) throw new Error(await errorText(attributeResponse));
    if (!structureResponse.ok) throw new Error(await errorText(structureResponse));
    setCatalog((await attributeResponse.json()) as AdminAttributeCatalog);
    setStructure((await structureResponse.json()) as AdminCatalogStructure);
  }, []);

  useEffect(() => {
    void load().catch((reason: unknown) =>
      setError(reason instanceof Error ? reason.message : "Özellikler alınamadı."));
  }, [load]);

  const nameOf = (item: { translations: { languageCode: string; name: string }[] }) =>
    item.translations.find((value) => value.languageCode === "tr")?.name ??
    item.translations[0]?.name ?? "Adsız";

  function resetAttribute() {
    setSelectedId(null); setCode(""); setDataType("Text"); setUnitDimension("");
    setActive(true); setTr(emptyText); setEn(emptyText); setEnglish(false);
    resetOption(); setMessage(null);
  }

  function selectAttribute(item: AdminAttribute) {
    const turkish = item.translations.find((value) => value.languageCode === "tr");
    const englishValue = item.translations.find((value) => value.languageCode === "en");
    setSelectedId(item.id); setCode(item.code); setDataType(item.dataType);
    setUnitDimension(item.unitDimension ?? ""); setActive(item.isActive);
    setTr({ name: turkish?.name ?? "" }); setEn({ name: englishValue?.name ?? "" });
    setEnglish(Boolean(englishValue)); resetOption();
  }

  function resetOption() {
    setOptionId(null); setOptionCode(""); setOptionSort(0);
    setOptionActive(true); setOptionTr(""); setOptionEn("");
  }

  function selectOption(item: AdminAttributeOption) {
    setOptionId(item.id); setOptionCode(item.code); setOptionSort(item.sortOrder);
    setOptionActive(item.isActive);
    setOptionTr(item.translations.find((value) => value.languageCode === "tr")?.name ?? "");
    setOptionEn(item.translations.find((value) => value.languageCode === "en")?.name ?? "");
  }

  async function request(url: string, method: "POST" | "PUT" | "DELETE", body?: object) {
    setBusy(true); setError(null); setMessage(null);
    try {
      const response = await fetch(url, {
        method,
        headers: body ? { "Content-Type": "application/json" } : undefined,
        body: body ? JSON.stringify(body) : undefined
      });
      if (!response.ok) throw new Error(await errorText(response));
      await load();
      return true;
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Kayıt tamamlanamadı.");
      return false;
    } finally {
      setBusy(false);
    }
  }

  async function saveAttribute(event: FormEvent) {
    event.preventDefault();
    const ok = await request(
      selectedId ? `/api/admin/catalog/attributes/${selectedId}` : "/api/admin/catalog/attributes",
      selectedId ? "PUT" : "POST",
      {
        code, dataType, unitDimension: dataType === "Number" ? unitDimension || null : null,
        isActive: active,
        translations: [{ languageCode: "tr", name: tr.name }, ...(english ? [{ languageCode: "en", name: en.name }] : [])]
      });
    if (ok) setMessage(selectedId ? "Özellik güncellendi." : "Özellik oluşturuldu.");
  }

  async function saveOption(event: FormEvent) {
    event.preventDefault();
    if (!selectedId) return;
    const ok = await request(
      optionId ? `/api/admin/catalog/attribute-options/${optionId}` : `/api/admin/catalog/attributes/${selectedId}/options`,
      optionId ? "PUT" : "POST",
      {
        code: optionCode, sortOrder: optionSort, isActive: optionActive,
        translations: [{ languageCode: "tr", name: optionTr }, ...(optionEn ? [{ languageCode: "en", name: optionEn }] : [])]
      });
    if (ok) { setMessage(optionId ? "Seçenek güncellendi." : "Seçenek oluşturuldu."); resetOption(); }
  }

  async function saveAssignment(event: FormEvent) {
    event.preventDefault();
    const ok = await request("/api/admin/catalog/category-attributes", "PUT", {
      categoryId, attributeId: assignmentAttributeId, isRequired: required,
      isFilterable: filterable, isVisibleOnProduct: visible,
      isVisibleOnComparison: comparison, sortOrder
    });
    if (ok) setMessage("Kategori özelliği kaydedildi.");
  }

  return <main className={styles.page}>
    <header className={styles.header}><div><p className={styles.eyebrow}>EKİPHAN · ADMIN</p>
      <h1>Dinamik özellikler</h1><p className={styles.lead}>Teknik özellik sözlüğünü, kontrollü seçenekleri ve kategori kurallarını yönetin.</p></div>
      <nav className={styles.nav} aria-label="Admin menüsü"><a href="/admin/catalog/products">Ürünler</a><a href="/admin/catalog/categories">Kategoriler</a><a href="/admin/catalog/variants">Varyantlar</a><a href="/admin/catalog/dictionaries">Etiketler ve birimler</a><a href="/admin/catalog/brands">Markalar</a><a href="/">Siteye dön</a></nav>
    </header>
    {error && <div className={styles.error} role="alert">{error}</div>}
    {message && <div className={styles.success} role="status">{message}</div>}
    <div className={styles.layout}>
      <section className={styles.panel}>
        <div className={styles.sectionHeader}><h2>Özellik sözlüğü</h2><button type="button" onClick={resetAttribute}>Yeni özellik</button></div>
        <div className={styles.productList}>{catalog.attributes.map((item) =>
          <button type="button" key={item.id} className={selectedId === item.id ? styles.selected : styles.product} onClick={() => selectAttribute(item)}>
            <strong>{nameOf(item)}</strong><span>{item.code} · {item.dataType}</span><small>{item.isActive ? "Aktif" : "Pasif"}</small>
          </button>)}</div>
        <form className={styles.editor} onSubmit={saveAttribute}>
          <h3>{selectedId ? "Özelliği düzenle" : "Yeni özellik"}</h3>
          <label>Kod<input required maxLength={100} value={code} onChange={(e) => setCode(e.target.value)} /></label>
          <label>Veri tipi<select value={dataType} disabled={Boolean(selectedId)} onChange={(e) => setDataType(e.target.value as AdminAttribute["dataType"])}>
            {["Text", "Number", "Boolean", "Option", "MultiOption"].map((value) => <option key={value}>{value}</option>)}
          </select></label>
          {dataType === "Number" && <label>Birim boyutu<input maxLength={50} value={unitDimension} onChange={(e) => setUnitDimension(e.target.value)} /></label>}
          <label>Türkçe ad<input required maxLength={150} value={tr.name} onChange={(e) => setTr({ name: e.target.value })} /></label>
          <label className={styles.checkbox}><input type="checkbox" checked={english} onChange={(e) => setEnglish(e.target.checked)} />İngilizce çeviri</label>
          {english && <label>İngilizce ad<input required maxLength={150} value={en.name} onChange={(e) => setEn({ name: e.target.value })} /></label>}
          <label className={styles.checkbox}><input type="checkbox" checked={active} onChange={(e) => setActive(e.target.checked)} />Aktif</label>
          <button disabled={busy}>{busy ? "Kaydediliyor…" : "Özelliği kaydet"}</button>
        </form>
        {selectedId && (dataType === "Option" || dataType === "MultiOption") && <form className={styles.editor} onSubmit={saveOption}>
          <h3>Seçenek sözlüğü</h3>
          <div className={styles.productList}>{catalog.attributes.find((item) => item.id === selectedId)?.options.map((item) =>
            <button type="button" key={item.id} className={optionId === item.id ? styles.selected : styles.product} onClick={() => selectOption(item)}>
              <strong>{nameOf(item)}</strong><span>{item.code}</span>
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
        <h2>Kategori kuralları</h2>
        <form className={styles.editor} onSubmit={saveAssignment}>
          <label>Kategori<select required value={categoryId} onChange={(e) => setCategoryId(e.target.value)}><option value="">Seçin</option>
            {structure.categories.map((item) => <option key={item.id} value={item.id}>{nameOf(item)}</option>)}
          </select></label>
          <label>Özellik<select required value={assignmentAttributeId} onChange={(e) => setAssignmentAttributeId(e.target.value)}><option value="">Seçin</option>
            {catalog.attributes.map((item) => <option key={item.id} value={item.id}>{nameOf(item)}</option>)}
          </select></label>
          <label>Sıra<input type="number" value={sortOrder} onChange={(e) => setSortOrder(e.target.valueAsNumber)} /></label>
          <label className={styles.checkbox}><input type="checkbox" checked={required} onChange={(e) => setRequired(e.target.checked)} />Zorunlu</label>
          <label className={styles.checkbox}><input type="checkbox" checked={filterable} onChange={(e) => setFilterable(e.target.checked)} />Filtrelenebilir</label>
          <label className={styles.checkbox}><input type="checkbox" checked={visible} onChange={(e) => setVisible(e.target.checked)} />Ürün sayfasında görünür</label>
          <label className={styles.checkbox}><input type="checkbox" checked={comparison} onChange={(e) => setComparison(e.target.checked)} />Karşılaştırmada görünür</label>
          <button disabled={busy}>Kuralı kaydet</button>
        </form>
        <div className={styles.productList}>{catalog.assignments.map((item) =>
          <div className={styles.product} key={`${item.categoryId}-${item.attributeId}`}>
            <strong>{nameOf(catalog.attributes.find((value) => value.id === item.attributeId) ?? { translations: [] })}</strong>
            <span>{nameOf(structure.categories.find((value) => value.id === item.categoryId) ?? { translations: [] })}</span>
            <small>{item.isRequired ? "Zorunlu" : "Opsiyonel"} · {item.isFilterable ? "Filtre" : "Filtre dışı"}</small>
          </div>)}</div>
      </section>
    </div>
  </main>;
}
