"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import type {
  AdminContentPage,
  ContentTranslation
} from "../../../../lib/content-types";
import styles from "./content.module.css";

const emptyTranslation = (languageCode: "tr" | "en"): ContentTranslation => ({
  languageCode,
  title: "",
  slug: "",
  summary: "",
  body: "",
  metaTitle: "",
  metaDescription: "",
  canonicalUrl: "",
  noIndex: false,
  noFollow: false,
  openGraphTitle: "",
  openGraphDescription: "",
  openGraphImageMediaId: null
});

const statusName: Record<number, string> = {
  1: "Draft",
  2: "Review",
  3: "Published",
  4: "Archived"
};

const contentTemplates = [
  { code: "ANA_SAYFA", slug: "ana-sayfa", label: "Ana sayfa giriş metni" },
  { code: "ANA_SAYFA_SAYACLAR", slug: "ana-sayfa-sayaclar", label: "Ana sayfa sayaçları" },
  { code: "ANA_SAYFA_NEDEN_EKIPHAN", slug: "ana-sayfa-neden-ekiphan", label: "Ana sayfa Neden Ekiphan" },
  { code: "ANA_SAYFA_HAKKIMIZDA", slug: "ana-sayfa-hakkimizda", label: "Ana sayfa Hakkımızda" },
  { code: "ANA_SAYFA_TEKLIF", slug: "ana-sayfa-teklif", label: "Ana sayfa teklif CTA" },
  { code: "HAKKIMIZDA", slug: "hakkimizda", label: "Hakkımızda" },
  { code: "MISYON_VIZYON", slug: "misyon-vizyon", label: "Misyon ve vizyon" },
  { code: "DEGERLER", slug: "degerler", label: "Değerler" },
  { code: "SERTIFIKALAR", slug: "sertifikalar", label: "Sertifikalar" },
  { code: "HIZMETLER", slug: "hizmetler", label: "Hizmetler" },
  { code: "REFERANSLAR", slug: "referanslar", label: "Referanslar" },
  { code: "ILETISIM", slug: "iletisim", label: "İletişim" },
  { code: "SHOWROOM", slug: "showroom", label: "Showroom" }
] as const;

function guidance(code: string): string | null {
  if (code === "ANA_SAYFA_SAYACLAR") {
    return "Her satıra Değer | Etiket yazın. Yalnız doğrulanmış kurumsal sayıları girin.";
  }
  if (code === "ANA_SAYFA_NEDEN_EKIPHAN") {
    return "Her kartı boş satırla ayırın; ilk satır kart başlığı, sonraki satırlar açıklamadır.";
  }
  if (code === "ANA_SAYFA_TEKLIF") {
    return "İçerik alanının ilk satırı Buton etiketi | /site-ici-url biçimindedir.";
  }
  return null;
}

async function errorOf(response: Response) {
  try {
    const value = (await response.json()) as { detail?: string };
    return value.detail ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function ContentAdminClient() {
  const [pages, setPages] = useState<AdminContentPage[]>([]);
  const [id, setId] = useState<string | null>(null);
  const [code, setCode] = useState("");
  const [status, setStatus] = useState("Draft");
  const [tr, setTr] = useState(emptyTranslation("tr"));
  const [en, setEn] = useState(emptyTranslation("en"));
  const [includeEnglish, setIncludeEnglish] = useState(false);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const response = await fetch("/api/admin/content/pages", {
        cache: "no-store"
      });
      if (!response.ok) throw new Error(await errorOf(response));
      setPages((await response.json()) as AdminContentPage[]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load().catch((reason: unknown) =>
      setError(reason instanceof Error ? reason.message : "İçerikler alınamadı.")
    );
  }, [load]);

  function reset() {
    setId(null);
    setCode("");
    setStatus("Draft");
    setTr(emptyTranslation("tr"));
    setEn(emptyTranslation("en"));
    setIncludeEnglish(false);
    setError(null);
  }

  function applyTemplate(nextCode: string) {
    const template = contentTemplates.find((item) => item.code === nextCode);
    if (!template) return;
    reset();
    setCode(template.code);
    setTr({ ...emptyTranslation("tr"), slug: template.slug });
    setEn({ ...emptyTranslation("en"), slug: `en-${template.slug}` });
  }

  function choose(page: AdminContentPage) {
    const trValue = page.translations.find((item) => item.languageCode === "tr");
    const enValue = page.translations.find((item) => item.languageCode === "en");
    setId(page.id);
    setCode(page.code);
    setStatus(statusName[page.status] ?? "Draft");
    setTr(trValue ?? emptyTranslation("tr"));
    setEn(enValue ?? emptyTranslation("en"));
    setIncludeEnglish(Boolean(enValue));
    setError(null);
    setMessage(null);
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const response = await fetch(
        id ? `/api/admin/content/pages/${id}` : "/api/admin/content/pages",
        {
          method: id ? "PUT" : "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            code,
            status,
            translations: [tr, ...(includeEnglish ? [en] : [])]
          })
        }
      );
      if (!response.ok) throw new Error(await errorOf(response));
      await load();
      if (!id) reset();
      setMessage(id ? "İçerik güncellendi." : "İçerik oluşturuldu.");
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Kayıt tamamlanamadı.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <div><p>EKİPHAN · İÇERİK</p><h1>Kurumsal sayfalar</h1></div>
        <nav><a href="/admin">Dashboard</a><a href="/">Siteye dön</a></nav>
      </header>
      {error && <div className={styles.error} role="alert">{error}</div>}
      {message && <div className={styles.success} role="status">{message}</div>}
      <div className={styles.layout}>
        <section className={styles.panel}>
          <div className={styles.panelTitle}><h2>Sayfalar</h2><button type="button" onClick={reset}>Yeni sayfa</button></div>
          <div className={styles.list}>
            {loading && <p className={styles.listState}>İçerikler yükleniyor…</p>}
            {!loading && pages.length === 0 &&
              <p className={styles.listState}>Henüz içerik sayfası yok.</p>}
            {!loading && pages.map((page) =>
            <button type="button" key={page.id} onClick={() => choose(page)} data-selected={id === page.id}>
              <strong>{page.translations.find((item) => item.languageCode === "tr")?.title ?? page.code}</strong>
              <span>{page.code}</span><small>{statusName[page.status]}</small>
            </button>)}</div>
        </section>
        <form className={styles.editor} onSubmit={save}>
          {!id && <label>Hazır içerik türü
            <select defaultValue="" onChange={(event) => applyTemplate(event.target.value)}>
              <option value="">Tür seçin (opsiyonel)</option>
              {contentTemplates.map((template) =>
                <option value={template.code} key={template.code}>{template.label}</option>)}
            </select>
          </label>}
          <div className={styles.row}>
            <label>İçerik kodu<input required maxLength={100} value={code} onChange={(e) => setCode(e.target.value)} /></label>
            <label>Durum<select value={status} onChange={(e) => setStatus(e.target.value)}><option>Draft</option><option>Review</option><option>Published</option><option>Archived</option></select></label>
          </div>
          {guidance(code) && <p className={styles.guidance}>{guidance(code)}</p>}
          <TranslationEditor value={tr} onChange={setTr} label="Türkçe" />
          <label className={styles.check}><input type="checkbox" checked={includeEnglish} onChange={(e) => setIncludeEnglish(e.target.checked)} />İngilizce içerik ekle</label>
          {includeEnglish && <TranslationEditor value={en} onChange={setEn} label="English" />}
          <button disabled={busy}>Sayfayı kaydet</button>
        </form>
      </div>
    </main>
  );
}

function TranslationEditor({
  value,
  onChange,
  label
}: {
  value: ContentTranslation;
  onChange: (value: ContentTranslation) => void;
  label: string;
}) {
  const set = (field: keyof ContentTranslation, next: string | boolean) =>
    onChange({ ...value, [field]: next });
  return <fieldset className={styles.translation}><legend>{label}</legend>
    <label>Başlık<input required maxLength={200} value={value.title} onChange={(e) => set("title", e.target.value)} /></label>
    <label>Slug<input required maxLength={200} pattern="[a-z0-9]+(?:-[a-z0-9]+)*" value={value.slug} onChange={(e) => set("slug", e.target.value)} /></label>
    <label>Özet<textarea maxLength={500} rows={2} value={value.summary ?? ""} onChange={(e) => set("summary", e.target.value)} /></label>
    <label>İçerik<textarea required maxLength={50000} rows={10} value={value.body} onChange={(e) => set("body", e.target.value)} /></label>
    <div className={styles.row}><label>Meta title<input maxLength={70} value={value.metaTitle ?? ""} onChange={(e) => set("metaTitle", e.target.value)} /></label>
      <label>Canonical URL<input maxLength={2048} value={value.canonicalUrl ?? ""} onChange={(e) => set("canonicalUrl", e.target.value)} /></label></div>
    <label>Meta description<textarea maxLength={170} rows={2} value={value.metaDescription ?? ""} onChange={(e) => set("metaDescription", e.target.value)} /></label>
    <div className={styles.row}><label className={styles.check}><input type="checkbox" checked={value.noIndex} onChange={(e) => set("noIndex", e.target.checked)} />Noindex</label>
      <label className={styles.check}><input type="checkbox" checked={value.noFollow} onChange={(e) => set("noFollow", e.target.checked)} />Nofollow</label></div>
  </fieldset>;
}
