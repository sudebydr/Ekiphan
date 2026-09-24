"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import type {
  AdminCatalogStructure,
  AdminCategoryDetail,
  AdminSectionDetail
} from "../../../../lib/admin-category-types";
import type { ProblemDetails } from "../../../../lib/admin-product-relation-types";
import styles from "../products/products.module.css";

type TranslationDraft = {
  name: string;
  slug: string;
  description: string;
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
  description: "", metaTitle: "", metaDescription: "", canonicalUrl: "",
  noIndex: false, noFollow: false, openGraphTitle: "",
  openGraphDescription: "", openGraphImageMediaId: null
};

async function readError(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as ProblemDetails;
    return problem.detail ?? problem.title ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

function translation(
  languageCode: "tr" | "en",
  value: TranslationDraft,
  withDescription: boolean
) {
  return {
    languageCode,
    name: value.name,
    slug: value.slug,
    description: withDescription ? value.description || null : null,
    ...(withDescription ? {
      metaTitle: value.metaTitle || null,
      metaDescription: value.metaDescription || null,
      canonicalUrl: value.canonicalUrl || null,
      noIndex: value.noIndex,
      noFollow: value.noFollow,
      openGraphTitle: value.openGraphTitle || null,
      openGraphDescription: value.openGraphDescription || null,
      openGraphImageMediaId: value.openGraphImageMediaId
    } : {})
  };
}

export function CategoryAdminClient() {
  const [structure, setStructure] = useState<AdminCatalogStructure>({
    sections: [],
    categories: []
  });
  const [selectedSectionId, setSelectedSectionId] = useState<string | null>(null);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null);
  const [sectionCode, setSectionCode] = useState("");
  const [sectionSort, setSectionSort] = useState(0);
  const [sectionPublished, setSectionPublished] = useState(false);
  const [sectionTr, setSectionTr] = useState<TranslationDraft>(emptyTranslation);
  const [sectionEn, setSectionEn] = useState<TranslationDraft>(emptyTranslation);
  const [sectionEnglish, setSectionEnglish] = useState(false);
  const [categorySectionId, setCategorySectionId] = useState("");
  const [parentId, setParentId] = useState("");
  const [categorySort, setCategorySort] = useState(0);
  const [categoryPublished, setCategoryPublished] = useState(false);
  const [categoryTr, setCategoryTr] = useState<TranslationDraft>(emptyTranslation);
  const [categoryEn, setCategoryEn] = useState<TranslationDraft>(emptyTranslation);
  const [categoryEnglish, setCategoryEnglish] = useState(false);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const response = await fetch("/api/admin/catalog/structure", {
        cache: "no-store"
      });
      if (!response.ok) throw new Error(await readError(response));
      setStructure((await response.json()) as AdminCatalogStructure);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load().catch((reason: unknown) =>
      setError(reason instanceof Error ? reason.message : "Katalog yapısı alınamadı.")
    );
  }, [load]);

  const unavailableParentIds = useMemo(() => {
    const result = new Set<string>();
    if (!selectedCategoryId) return result;
    result.add(selectedCategoryId);
    let changed = true;
    while (changed) {
      changed = false;
      for (const item of structure.categories) {
        if (item.parentId && result.has(item.parentId) && !result.has(item.id)) {
          result.add(item.id);
          changed = true;
        }
      }
    }
    return result;
  }, [selectedCategoryId, structure.categories]);

  const parentOptions = useMemo(
    () => structure.categories.filter(
      (item) =>
        item.productSectionId === categorySectionId &&
        !unavailableParentIds.has(item.id)
    ),
    [categorySectionId, structure.categories, unavailableParentIds]
  );

  const categoryRows = useMemo(() => {
    const result: Array<{ category: AdminCategoryDetail; depth: number }> = [];
    const visited = new Set<string>();
    const children = new Map<string, AdminCategoryDetail[]>();
    for (const category of structure.categories) {
      const key = `${category.productSectionId}:${category.parentId ?? "root"}`;
      children.set(key, [...(children.get(key) ?? []), category]);
    }
    const append = (category: AdminCategoryDetail, depth: number) => {
      if (visited.has(category.id)) return;
      visited.add(category.id);
      result.push({ category, depth });
      const key = `${category.productSectionId}:${category.id}`;
      for (const child of children.get(key) ?? []) append(child, depth + 1);
    };
    for (const section of structure.sections) {
      const key = `${section.id}:root`;
      for (const root of children.get(key) ?? []) append(root, 0);
    }
    for (const category of structure.categories) append(category, 0);
    return result;
  }, [structure]);

  function resetSection() {
    setSelectedSectionId(null);
    setSectionCode("");
    setSectionSort(0);
    setSectionPublished(false);
    setSectionTr(emptyTranslation);
    setSectionEn(emptyTranslation);
    setSectionEnglish(false);
    setMessage(null);
  }

  function resetCategory() {
    setSelectedCategoryId(null);
    setCategorySectionId(structure.sections[0]?.id ?? "");
    setParentId("");
    setCategorySort(0);
    setCategoryPublished(false);
    setCategoryTr(emptyTranslation);
    setCategoryEn(emptyTranslation);
    setCategoryEnglish(false);
    setMessage(null);
  }

  function chooseSection(section: AdminSectionDetail) {
    const tr = section.translations.find((item) => item.languageCode === "tr");
    const en = section.translations.find((item) => item.languageCode === "en");
    setSelectedSectionId(section.id);
    setSectionCode(section.code);
    setSectionSort(section.sortOrder);
    setSectionPublished(section.isPublished);
    setSectionTr(toDraft(tr));
    setSectionEn(toDraft(en));
    setSectionEnglish(Boolean(en));
    setMessage(null);
  }

  function chooseCategory(category: AdminCategoryDetail) {
    const tr = category.translations.find((item) => item.languageCode === "tr");
    const en = category.translations.find((item) => item.languageCode === "en");
    setSelectedCategoryId(category.id);
    setCategorySectionId(category.productSectionId);
    setParentId(category.parentId ?? "");
    setCategorySort(category.sortOrder);
    setCategoryPublished(category.isPublished);
    setCategoryTr(toDraft(tr));
    setCategoryEn(toDraft(en));
    setCategoryEnglish(Boolean(en));
    setMessage(null);
  }

  async function saveSection(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await save(
      selectedSectionId
        ? `/api/admin/catalog/sections/${selectedSectionId}`
        : "/api/admin/catalog/sections",
      selectedSectionId ? "PUT" : "POST",
      {
        code: sectionCode,
        sortOrder: sectionSort,
        isPublished: sectionPublished,
        translations: [
          translation("tr", sectionTr, false),
          ...(sectionEnglish ? [translation("en", sectionEn, false)] : [])
        ]
      },
      selectedSectionId ? "Ürün bölümü güncellendi." : "Ürün bölümü oluşturuldu."
    );
  }

  async function saveCategory(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await save(
      selectedCategoryId
        ? `/api/admin/catalog/categories/${selectedCategoryId}`
        : "/api/admin/catalog/categories",
      selectedCategoryId ? "PUT" : "POST",
      {
        productSectionId: categorySectionId,
        parentId: parentId || null,
        sortOrder: categorySort,
        isPublished: categoryPublished,
        translations: [
          translation("tr", categoryTr, true),
          ...(categoryEnglish ? [translation("en", categoryEn, true)] : [])
        ]
      },
      selectedCategoryId ? "Kategori güncellendi." : "Kategori oluşturuldu."
    );
  }

  async function archiveCategory() {
    if (!selectedCategoryId ||
      !window.confirm("Bu kategori güvenli biçimde pasife alınsın mı?")) return;
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const response = await fetch(
        `/api/admin/catalog/categories/${selectedCategoryId}`,
        { method: "DELETE" }
      );
      if (!response.ok) throw new Error(await readError(response));
      setCategoryPublished(false);
      setMessage("Kategori pasife alındı; ilişkili veri silinmedi.");
      await load();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Kategori pasife alınamadı.");
    } finally {
      setBusy(false);
    }
  }

  async function save(
    url: string,
    method: "POST" | "PUT",
    body: object,
    successMessage: string
  ) {
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const response = await fetch(url, {
        method,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
      });
      if (!response.ok) throw new Error(await readError(response));
      setMessage(successMessage);
      await load();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Kayıt tamamlanamadı.");
    } finally {
      setBusy(false);
    }
  }

  const nameOf = (item: AdminSectionDetail | AdminCategoryDetail) =>
    item.translations.find((value) => value.languageCode === "tr")?.name ??
    item.translations[0]?.name ??
    "Adsız kayıt";

  return <main className={styles.page}>
    <header className={styles.header}>
      <div>
        <p className={styles.eyebrow}>EKİPHAN · ADMIN</p>
        <h1>Kategori yönetimi</h1>
        <p className={styles.lead}>
          Ürün bölümlerini ve çok seviyeli kategori ağacını birlikte yönetin.
        </p>
      </div>
      <nav className={styles.nav} aria-label="Admin menüsü">
        <a href="/admin/catalog/products">Ürünler</a>
        <a href="/admin/catalog/attributes">Özellikler</a>
        <a href="/admin/catalog/variants">Varyantlar</a>
        <a href="/admin/media">Medya</a>
        <a href="/admin/catalog/brands">Markalar</a>
        <a href="/admin/catalog/relations">Ürün ilişkileri</a>
        <a href="/">Siteye dön</a>
      </nav>
    </header>
    {error && <div className={styles.error} role="alert">{error}</div>}
    {message && <div className={styles.success} role="status">{message}</div>}
    <div className={styles.layout}>
      <section className={styles.panel}>
        <div className={styles.sectionHeader}>
          <h2>Ürün bölümleri</h2>
          <button type="button" onClick={resetSection}>Yeni bölüm</button>
        </div>
        <div className={styles.productList}>
          {loading && <p className={styles.listState}>Katalog yapısı yükleniyor…</p>}
          {!loading && structure.sections.length === 0 &&
            <p className={styles.listState}>Henüz ürün bölümü yok.</p>}
          {!loading && structure.sections.map((section) =>
            <button type="button" key={section.id} disabled={busy}
              className={selectedSectionId === section.id ? styles.selected : styles.product}
              onClick={() => chooseSection(section)}>
              <strong>{nameOf(section)}</strong>
              <span>{section.code}</span>
              <small>{section.isPublished ? "Yayında" : "Taslak"}</small>
            </button>
          )}
        </div>
        <form className={styles.editor} onSubmit={saveSection}>
          <h3>{selectedSectionId ? "Bölümü düzenle" : "Yeni bölüm"}</h3>
          <label>Kod<input required maxLength={50} value={sectionCode}
            onChange={(event) => setSectionCode(event.target.value)} /></label>
          <label>Sıra<input type="number" value={sectionSort}
            onChange={(event) => setSectionSort(event.target.valueAsNumber)} /></label>
          <label className={styles.checkbox}><input type="checkbox"
            checked={sectionPublished}
            onChange={(event) => setSectionPublished(event.target.checked)} />Yayında</label>
          <TranslationFields title="Türkçe" prefix="section-tr"
            value={sectionTr} setValue={setSectionTr} />
          <label className={styles.checkbox}><input type="checkbox"
            checked={sectionEnglish}
            onChange={(event) => setSectionEnglish(event.target.checked)} />İngilizce çeviriyi yönet</label>
          {sectionEnglish && <TranslationFields title="İngilizce" prefix="section-en"
            value={sectionEn} setValue={setSectionEn} />}
          <button disabled={busy}>{busy ? "Kaydediliyor…" : "Bölümü kaydet"}</button>
        </form>
      </section>
      <section className={styles.panel}>
        <div className={styles.sectionHeader}>
          <h2>Kategoriler</h2>
          <button type="button" onClick={resetCategory}
            disabled={structure.sections.length === 0}>Yeni kategori</button>
        </div>
        <div className={styles.productList}>
          {loading && <p className={styles.listState}>Kategori ağacı yükleniyor…</p>}
          {!loading && categoryRows.length === 0 &&
            <p className={styles.listState}>Henüz kategori yok.</p>}
          {!loading && categoryRows.map(({ category, depth }) =>
            <button type="button" key={category.id} disabled={busy}
              className={selectedCategoryId === category.id ? styles.selected : styles.product}
              style={{ marginInlineStart: `${depth * 0.8}rem` }}
              onClick={() => chooseCategory(category)}>
              <strong>{depth > 0 ? "↳ " : ""}{nameOf(category)}</strong>
              <span>Seviye {depth + 1} · {category.productCount} ürün</span>
              <small>{category.isPublished ? "Yayında" : "Taslak"}</small>
            </button>
          )}
        </div>
        <form className={styles.editor} onSubmit={saveCategory}>
          <h3>{selectedCategoryId ? "Kategoriyi düzenle" : "Yeni kategori"}</h3>
          <label>Ürün bölümü<select required value={categorySectionId}
            onChange={(event) => { setCategorySectionId(event.target.value); setParentId(""); }}>
            <option value="">Bölüm seçin</option>
            {structure.sections.map((section) =>
              <option key={section.id} value={section.id}>{nameOf(section)}</option>
            )}
          </select></label>
          <label>Üst kategori<select value={parentId}
            onChange={(event) => setParentId(event.target.value)}>
            <option value="">Kök kategori</option>
            {parentOptions.map((category) =>
              <option key={category.id} value={category.id}>{nameOf(category)}</option>
            )}
          </select></label>
          <small>Kategori ağacı en fazla 5 seviye olabilir.</small>
          <label>Sıra<input type="number" value={categorySort}
            onChange={(event) => setCategorySort(event.target.valueAsNumber)} /></label>
          <label className={styles.checkbox}><input type="checkbox"
            checked={categoryPublished}
            onChange={(event) => setCategoryPublished(event.target.checked)} />Yayında</label>
          <TranslationFields title="Türkçe" prefix="category-tr"
            value={categoryTr} setValue={setCategoryTr} withDescription />
          <label className={styles.checkbox}><input type="checkbox"
            checked={categoryEnglish}
            onChange={(event) => setCategoryEnglish(event.target.checked)} />İngilizce çeviriyi yönet</label>
          {categoryEnglish && <TranslationFields title="İngilizce" prefix="category-en"
            value={categoryEn} setValue={setCategoryEn} withDescription />}
          <div className={styles.actions}>
            <button disabled={busy || !categorySectionId}>
              {busy ? "Kaydediliyor…" : "Kategoriyi kaydet"}
            </button>
            {selectedCategoryId && categoryPublished &&
              <button type="button" className={styles.danger} disabled={busy}
                onClick={() => void archiveCategory()}>Güvenli pasife al</button>}
          </div>
          {selectedCategoryId &&
            <a href="/admin/media">Kategori görseli veya ikonunu medya kütüphanesinde yönet</a>}
        </form>
      </section>
    </div>
  </main>;
}

function TranslationFields({
  title,
  prefix,
  value,
  setValue,
  withDescription = false
}: {
  title: string;
  prefix: string;
  value: TranslationDraft;
  setValue: (value: TranslationDraft) => void;
  withDescription?: boolean;
}) {
  const update = (key: keyof TranslationDraft, next: string) =>
    setValue({ ...value, [key]: next });
  return <fieldset>
    <legend>{title}</legend>
    <label htmlFor={`${prefix}-name`}>Ad<input id={`${prefix}-name`} required
      maxLength={200} value={value.name}
      onChange={(event) => update("name", event.target.value)} /></label>
    <label htmlFor={`${prefix}-slug`}>Slug<input id={`${prefix}-slug`} required
      maxLength={250} value={value.slug}
      onChange={(event) => update("slug", event.target.value)} /></label>
    {withDescription && <label htmlFor={`${prefix}-description`}>Açıklama
      <textarea id={`${prefix}-description`} maxLength={4000}
        value={value.description}
        onChange={(event) => update("description", event.target.value)} />
    </label>}
  </fieldset>;
}

function toDraft(value?: {
  name: string;
  slug: string;
  description?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  canonicalUrl?: string | null;
  noIndex?: boolean;
  noFollow?: boolean;
  openGraphTitle?: string | null;
  openGraphDescription?: string | null;
  openGraphImageMediaId?: string | null;
}): TranslationDraft {
  return value
    ? { name: value.name, slug: value.slug, description: value.description ?? "", metaTitle: value.metaTitle ?? "", metaDescription: value.metaDescription ?? "", canonicalUrl: value.canonicalUrl ?? "", noIndex: value.noIndex ?? false, noFollow: value.noFollow ?? false, openGraphTitle: value.openGraphTitle ?? "", openGraphDescription: value.openGraphDescription ?? "", openGraphImageMediaId: value.openGraphImageMediaId ?? null }
    : emptyTranslation;
}
