"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import type {
  AdminBrandDetail,
  AdminBrandPage,
  AdminBrandTranslation
} from "../../../../lib/admin-brand-types";
import type { ProblemDetails } from "../../../../lib/admin-product-relation-types";
import styles from "../products/products.module.css";

type TranslationDraft = { description: string; slug: string };
const emptyTranslation: TranslationDraft = { description: "", slug: "" };

async function readError(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as ProblemDetails;
    return problem.detail ?? problem.title ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function BrandAdminClient() {
  const [brands, setBrands] = useState<AdminBrandPage | null>(null);
  const [search, setSearch] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [websiteUrl, setWebsiteUrl] = useState("");
  const [sortOrder, setSortOrder] = useState(0);
  const [isPublished, setIsPublished] = useState(false);
  const [tr, setTr] = useState<TranslationDraft>(emptyTranslation);
  const [en, setEn] = useState<TranslationDraft>(emptyTranslation);
  const [includeEnglish, setIncludeEnglish] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadBrands = useCallback(async (term = "") => {
    const query = new URLSearchParams({
      page: "1",
      pageSize: "100",
      language: "tr"
    });
    if (term.trim()) query.set("search", term.trim());
    const response = await fetch(`/api/admin/catalog/brands?${query}`, {
      cache: "no-store"
    });
    if (!response.ok) throw new Error(await readError(response));
    setBrands((await response.json()) as AdminBrandPage);
  }, []);

  useEffect(() => {
    void loadBrands().catch((reason: unknown) =>
      setError(reason instanceof Error ? reason.message : "Markalar alınamadı.")
    );
  }, [loadBrands]);

  function reset() {
    setSelectedId(null);
    setName("");
    setWebsiteUrl("");
    setSortOrder(0);
    setIsPublished(false);
    setTr(emptyTranslation);
    setEn(emptyTranslation);
    setIncludeEnglish(false);
    setError(null);
    setMessage(null);
  }

  async function selectBrand(id: string) {
    setBusy(true);
    setError(null);
    try {
      const response = await fetch(`/api/admin/catalog/brands/${id}`, {
        cache: "no-store"
      });
      if (!response.ok) throw new Error(await readError(response));
      const brand = (await response.json()) as AdminBrandDetail;
      const turkish = brand.translations.find((item) => item.languageCode === "tr");
      const english = brand.translations.find((item) => item.languageCode === "en");
      setSelectedId(brand.id);
      setName(brand.name);
      setWebsiteUrl(brand.websiteUrl ?? "");
      setSortOrder(brand.sortOrder);
      setIsPublished(brand.isPublished);
      setTr(toDraft(turkish));
      setEn(toDraft(english));
      setIncludeEnglish(Boolean(english));
      setMessage(null);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Marka alınamadı.");
    } finally {
      setBusy(false);
    }
  }

  async function searchBrands(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    try {
      await loadBrands(search);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Arama başarısız.");
    } finally {
      setBusy(false);
    }
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const response = await fetch(
        selectedId
          ? `/api/admin/catalog/brands/${selectedId}`
          : "/api/admin/catalog/brands",
        {
          method: selectedId ? "PUT" : "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            name,
            websiteUrl: websiteUrl || null,
            sortOrder,
            isPublished,
            translations: [
              { languageCode: "tr", ...tr },
              ...(includeEnglish
                ? [{ languageCode: "en", ...en }]
                : [])
            ]
          })
        }
      );
      if (!response.ok) throw new Error(await readError(response));
      const saved = (await response.json()) as AdminBrandDetail;
      setSelectedId(saved.id);
      setMessage(selectedId ? "Marka güncellendi." : "Marka oluşturuldu.");
      await loadBrands(search);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Marka kaydedilemedi.");
    } finally {
      setBusy(false);
    }
  }

  async function archive() {
    if (!selectedId || !window.confirm("Bu marka güvenli biçimde pasife alınsın mı?")) return;
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const response = await fetch(`/api/admin/catalog/brands/${selectedId}`, {
        method: "DELETE"
      });
      if (!response.ok) throw new Error(await readError(response));
      setIsPublished(false);
      setMessage("Marka pasife alındı; ilişkili veri silinmedi.");
      await loadBrands(search);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Marka pasife alınamadı.");
    } finally {
      setBusy(false);
    }
  }

  return <main className={styles.page}>
    <header className={styles.header}><div><p className={styles.eyebrow}>EKİPHAN · ADMIN</p><h1>Marka yönetimi</h1><p className={styles.lead}>Marka kimliğini, resmi web adresini ve çok dilli açıklamaları yönetin.</p></div>
      <nav className={styles.nav} aria-label="Admin menüsü"><a href="/admin/catalog/products">Ürünler</a><a href="/admin/catalog/categories">Kategoriler</a><a href="/admin/catalog/attributes">Özellikler</a><a href="/admin/media">Medya</a><a href="/admin/catalog/relations">Ürün ilişkileri</a><a href="/admin/imports">Ürün importu</a><a href="/">Siteye dön</a></nav>
    </header>
    {error && <div className={styles.error} role="alert">{error}</div>}
    {message && <div className={styles.success} role="status">{message}</div>}
    <div className={styles.layout}>
      <section className={styles.panel}><div className={styles.sectionHeader}><h2>Markalar</h2><button type="button" onClick={reset}>Yeni marka</button></div>
        <form className={styles.search} onSubmit={searchBrands}><label htmlFor="brand-search">Marka adı</label><div><input id="brand-search" maxLength={100} value={search} onChange={(e) => setSearch(e.target.value)} /><button disabled={busy}>Ara</button></div></form>
        <div className={styles.productList}>{brands?.items.map((brand) => <button type="button" key={brand.id} disabled={busy} className={selectedId === brand.id ? styles.selected : styles.product} onClick={() => void selectBrand(brand.id)}><strong>{brand.name}</strong><span>{brand.slug}</span><small>{brand.isPublished ? "Yayında" : "Taslak"}</small></button>)}</div>
      </section>
      <section className={styles.panel}><h2>{selectedId ? "Markayı düzenle" : "Yeni marka"}</h2>
        <form className={styles.editor} onSubmit={save}>
          <label>Marka adı<input required maxLength={150} value={name} onChange={(e) => setName(e.target.value)} /></label>
          <label>Resmi HTTPS web adresi<input type="url" pattern="https://.*" value={websiteUrl} onChange={(e) => setWebsiteUrl(e.target.value)} /></label>
          <label>Yönetim sırası<input type="number" value={sortOrder} onChange={(e) => setSortOrder(e.target.valueAsNumber)} /></label>
          <label className={styles.checkbox}><input type="checkbox" checked={isPublished} onChange={(e) => setIsPublished(e.target.checked)} />Yayında</label>
          <TranslationFields title="Türkçe" prefix="tr" value={tr} setValue={setTr} />
          <label className={styles.checkbox}><input type="checkbox" checked={includeEnglish} onChange={(e) => setIncludeEnglish(e.target.checked)} />İngilizce çeviriyi yönet</label>
          {includeEnglish && <TranslationFields title="İngilizce" prefix="en" value={en} setValue={setEn} />}
          <div className={styles.actions}><button disabled={busy}>{busy ? "Kaydediliyor…" : "Kaydet"}</button>
            {selectedId && isPublished && <button type="button" className={styles.danger}
              disabled={busy} onClick={() => void archive()}>Güvenli pasife al</button>}
          </div>
          {selectedId && <a href="/admin/media">Marka logosunu medya kütüphanesinde yönet</a>}
        </form>
      </section>
    </div>
  </main>;
}

function TranslationFields({ title, prefix, value, setValue }: { title: string; prefix: string; value: TranslationDraft; setValue: (value: TranslationDraft) => void }) {
  return <fieldset><legend>{title}</legend>
    <label htmlFor={`${prefix}-slug`}>Slug<input id={`${prefix}-slug`} required maxLength={200} value={value.slug} onChange={(e) => setValue({ ...value, slug: e.target.value })} /></label>
    <label htmlFor={`${prefix}-description`}>Açıklama<textarea id={`${prefix}-description`} required maxLength={4000} value={value.description} onChange={(e) => setValue({ ...value, description: e.target.value })} /></label>
  </fieldset>;
}

function toDraft(value?: AdminBrandTranslation): TranslationDraft {
  return value ? { description: value.description, slug: value.slug } : emptyTranslation;
}
