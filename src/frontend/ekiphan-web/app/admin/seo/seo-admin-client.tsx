"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import type { AdminMediaLibrary } from "../../../lib/admin-media-types";
import type { AdminSeoDetail, AdminSeoHealth, AdminSeoPage, AdminSeoRow, AdminSeoTranslation, SeoContentType } from "../../../lib/admin-seo-types";
import { useAdminSession } from "../admin-session-guard";
import styles from "./seo.module.css";

const labels: Record<SeoContentType, string> = { Product: "Ürün", Category: "Kategori", ContentPage: "Kurumsal sayfa", PressRelease: "Basın içeriği" };
const slugify = (value: string) => value.toLocaleLowerCase("tr-TR").normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/ı/g, "i").replace(/ğ/g, "g").replace(/ş/g, "s").replace(/ç/g, "c").replace(/ö/g, "o").replace(/ü/g, "u").replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
async function readError(response: Response) { try { const value = await response.json() as { detail?: string }; return value.detail ?? "İşlem tamamlanamadı."; } catch { return "İşlem tamamlanamadı."; } }

export function SeoAdminClient() {
  const router = useRouter(); const searchParams = useSearchParams();
  const { session } = useAdminSession();
  const canCatalog = session.user.permissions.includes("catalog.manage");
  const canContent = session.user.permissions.includes("content.manage");
  const [pageData, setPageData] = useState<AdminSeoPage | null>(null);
  const [health, setHealth] = useState<AdminSeoHealth | null>(null);
  const [selected, setSelected] = useState<AdminSeoDetail | null>(null);
  const [draft, setDraft] = useState<AdminSeoTranslation[]>([]);
  const [tab, setTab] = useState<"tr" | "en">("tr");
  const [media, setMedia] = useState<AdminMediaLibrary["assets"]>([]);
  const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false);
  const [dirty, setDirty] = useState(false); const [error, setError] = useState<string | null>(null); const [message, setMessage] = useState<string | null>(null);

  const query = searchParams.toString();
  const load = useCallback(async () => {
    setLoading(true); setError(null);
    try {
      const [listResponse, healthResponse] = await Promise.all([
        fetch(`/api/admin/seo${query ? `?${query}` : ""}`, { cache: "no-store" }),
        fetch("/api/admin/seo/summary", { cache: "no-store" })
      ]);
      if (!listResponse.ok) throw new Error(await readError(listResponse));
      if (!healthResponse.ok) throw new Error(await readError(healthResponse));
      setPageData(await listResponse.json() as AdminSeoPage);
      setHealth(await healthResponse.json() as AdminSeoHealth);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "SEO verileri alınamadı."); }
    finally { setLoading(false); }
  }, [query]);
  useEffect(() => { void load(); }, [load]);
  useEffect(() => { if (!dirty) return; const warn = (event: BeforeUnloadEvent) => event.preventDefault(); window.addEventListener("beforeunload", warn); return () => window.removeEventListener("beforeunload", warn); }, [dirty]);

  function setFilter(name: string, value: string) { const next = new URLSearchParams(searchParams); value ? next.set(name, value) : next.delete(name); next.set("page", "1"); router.replace(`/admin/seo?${next}`); }
  async function openEditor(row: AdminSeoRow) {
    if (dirty && !window.confirm("Kaydedilmemiş değişiklikler silinsin mi?")) return;
    setError(null); const response = await fetch(`/api/admin/seo/${row.contentType.toLowerCase()}/${row.contentId}`, { cache: "no-store" });
    if (!response.ok) { setError(await readError(response)); return; }
    const detail = await response.json() as AdminSeoDetail; setSelected(detail); setDraft(detail.translations); setTab(detail.translations.some((x) => x.language === "tr") ? "tr" : "en"); setDirty(false);
    if (media.length === 0) { const mediaResponse = await fetch("/api/admin/media-library?assetType=Image&status=Active&pageSize=100", { cache: "no-store" }); if (mediaResponse.ok) setMedia((await mediaResponse.json() as AdminMediaLibrary).assets); }
  }
  function update(field: keyof AdminSeoTranslation, value: string | boolean | null) { setDraft((items) => items.map((item) => item.language === tab ? { ...item, [field]: value } : item)); setDirty(true); setMessage(null); }
  async function save(event: FormEvent) {
    event.preventDefault(); if (!selected) return; setBusy(true); setError(null);
    try {
      const response = await fetch(`/api/admin/seo/${selected.contentType.toLowerCase()}/${selected.contentId}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ translations: draft }) });
      if (!response.ok) throw new Error(await readError(response));
      const detail = await response.json() as AdminSeoDetail; setSelected(detail); setDraft(detail.translations); setDirty(false); setMessage("SEO alanları kaydedildi."); await load();
    } catch (reason) { setError(reason instanceof Error ? reason.message : "SEO kaydedilemedi."); } finally { setBusy(false); }
  }
  const active = draft.find((x) => x.language === tab);
  const allowedTypes = useMemo(() => [{ value: "Product", label: "Ürün", allowed: canCatalog }, { value: "Category", label: "Kategori", allowed: canCatalog }, { value: "ContentPage", label: "Kurumsal sayfa", allowed: canContent }, { value: "PressRelease", label: "Basın içeriği", allowed: canContent }].filter((x) => x.allowed), [canCatalog, canContent]);
  return <main className={styles.page}>
    <header className={styles.header}><div><p>EKİPHAN · SEO</p><h1>SEO Yönetimi</h1><span>Arama görünürlüğünü gerçek içerik verisi üzerinden yönetin.</span></div><a href="/admin">Dashboard</a></header>
    {error && <div className={styles.error} role="alert">{error}</div>}{message && <div className={styles.success} role="status">{message}</div>}
    <section className={styles.health} aria-label="SEO sağlık özeti">
      {health ? [["Indexlenebilir", health.totalIndexable], ["Eksik başlık", health.missingMetaTitle], ["Eksik açıklama", health.missingMetaDescription], ["Eksik OG", health.missingOpenGraphImage], ["Duplicate slug", health.duplicateSlugs], ["Duplicate title", health.duplicateMetaTitles], ["Noindex", health.noIndex], ["Eksik İngilizce", health.missingEnglish]].map(([name, value]) => <article key={name}><strong>{value}</strong><span>{name}</span></article>) : Array.from({ length: 8 }, (_, index) => <article className={styles.skeleton} key={index} />)}
    </section>
    <section className={styles.filters} aria-label="SEO filtreleri">
      <label>İçerik türü<select value={searchParams.get("contentType") ?? ""} onChange={(e) => setFilter("contentType", e.target.value)}><option value="">Tümü</option>{allowedTypes.map((x) => <option value={x.value} key={x.value}>{x.label}</option>)}</select></label>
      <label>Dil<select value={searchParams.get("language") ?? ""} onChange={(e) => setFilter("language", e.target.value)}><option value="">Tümü</option><option value="tr">TR</option><option value="en">EN</option></select></label>
      <label>Yayın<select value={searchParams.get("published") ?? ""} onChange={(e) => setFilter("published", e.target.value)}><option value="">Tümü</option><option value="true">Yayında</option><option value="false">Taslak</option></select></label>
      <label>Eksik alan<select value={searchParams.get("missingMetaTitle") ? "missingMetaTitle" : searchParams.get("missingMetaDescription") ? "missingMetaDescription" : searchParams.get("missingOpenGraphImage") ? "missingOpenGraphImage" : ""} onChange={(e) => { ["missingMetaTitle", "missingMetaDescription", "missingOpenGraphImage"].forEach((key) => setFilter(key, key === e.target.value ? "true" : "")); }}><option value="">Tümü</option><option value="missingMetaTitle">Meta title</option><option value="missingMetaDescription">Meta description</option><option value="missingOpenGraphImage">OG görseli</option></select></label>
      <label>Durum<select value={searchParams.get("noIndex") ? "noIndex" : searchParams.get("duplicateSlug") ? "duplicateSlug" : searchParams.get("duplicateMetaTitle") ? "duplicateMetaTitle" : ""} onChange={(e) => { ["noIndex", "duplicateSlug", "duplicateMetaTitle"].forEach((key) => setFilter(key, key === e.target.value ? "true" : "")); }}><option value="">Tümü</option><option value="noIndex">Noindex</option><option value="duplicateSlug">Duplicate slug</option><option value="duplicateMetaTitle">Duplicate title</option></select></label>
      <label>Arama<input defaultValue={searchParams.get("search") ?? ""} onKeyDown={(e) => { if (e.key === "Enter") setFilter("search", e.currentTarget.value); }} placeholder="İsim, slug veya title" /></label>
      <label>Sıralama<select value={searchParams.get("sort") ?? "updated-desc"} onChange={(e) => setFilter("sort", e.target.value)}><option value="updated-desc">Son güncellenen</option><option value="name">İsim</option><option value="score">SEO skoru</option><option value="updated">En eski</option></select></label>
    </section>
    <section className={styles.tablePanel}><div className={styles.tableTitle}><h2>SEO kayıtları</h2><span>{pageData?.totalCount ?? 0} kayıt</span></div>
      {loading && <p className={styles.state}>Kayıtlar yükleniyor…</p>}{!loading && pageData?.items.length === 0 && <p className={styles.state}>Filtrelere uygun SEO kaydı yok.</p>}
      {!loading && pageData && pageData.items.length > 0 && <div className={styles.tableWrap}><table><thead><tr><th>Tür / içerik</th><th>Dil</th><th>Slug</th><th>Meta title</th><th>Skor</th><th>Index</th><th>Yayın</th><th>Güncelleme</th><th /></tr></thead><tbody>{pageData.items.map((row) => <tr key={`${row.contentType}-${row.contentId}-${row.language}`}><td><strong>{row.contentName}</strong><small>{labels[row.contentType]}</small></td><td>{row.language.toUpperCase()}</td><td><code>{row.slug}</code></td><td>{row.metaTitle ?? <em>Eksik</em>}</td><td><span className={row.score >= 75 ? styles.good : row.score >= 50 ? styles.warn : styles.bad}>{row.score}</span></td><td>{row.noIndex ? "Noindex" : "Index"}</td><td>{row.published ? "Yayında" : "Taslak"}</td><td>{new Date(row.updatedAt).toLocaleDateString("tr-TR")}</td><td><button type="button" onClick={() => void openEditor(row)}>Düzenle</button></td></tr>)}</tbody></table></div>}
      {pageData && pageData.totalCount > pageData.pageSize && <nav className={styles.pagination} aria-label="Sayfalama"><button disabled={pageData.page <= 1} onClick={() => setFilter("page", String(pageData.page - 1))}>Önceki</button><span>{pageData.page} / {Math.ceil(pageData.totalCount / pageData.pageSize)}</span><button disabled={pageData.page >= Math.ceil(pageData.totalCount / pageData.pageSize)} onClick={() => setFilter("page", String(pageData.page + 1))}>Sonraki</button></nav>}
    </section>
    {selected && active && <div className={styles.modalBackdrop} role="presentation"><form className={styles.editor} onSubmit={save} aria-label="SEO düzenleme"><div className={styles.editorHead}><div><small>{labels[selected.contentType]}</small><h2>{active.contentName}</h2></div><button type="button" onClick={() => { if (!dirty || window.confirm("Kaydedilmemiş değişiklikler silinsin mi?")) { setSelected(null); setDirty(false); } }}>Kapat</button></div>
      <div className={styles.tabs} role="tablist">{draft.map((item) => <button type="button" role="tab" aria-selected={tab === item.language} key={item.language} onClick={() => setTab(item.language)}>{item.language.toUpperCase()}</button>)}</div>
      <div className={styles.fields}><label>Meta title <span>{active.metaTitle?.length ?? 0}/70</span><input maxLength={70} value={active.metaTitle ?? ""} onChange={(e) => update("metaTitle", e.target.value)} /></label><label>Meta description <span>{active.metaDescription?.length ?? 0}/320</span><textarea maxLength={320} rows={3} value={active.metaDescription ?? ""} onChange={(e) => update("metaDescription", e.target.value)} /></label>
      <label>Slug<div className={styles.inline}><input required maxLength={250} pattern="[a-z0-9]+(?:-[a-z0-9]+)*" value={active.slug} onChange={(e) => update("slug", e.target.value)} /><button type="button" onClick={() => update("slug", slugify(active.contentName))}>Oluştur</button></div></label><label>Canonical URL<input maxLength={2048} value={active.canonicalUrl ?? ""} placeholder="/urunler/ornek veya https://…" onChange={(e) => update("canonicalUrl", e.target.value)} /></label>
      <label>Open Graph title <span>{active.openGraphTitle?.length ?? 0}/95</span><input maxLength={95} value={active.openGraphTitle ?? ""} onChange={(e) => update("openGraphTitle", e.target.value)} /></label><label>Open Graph description <span>{active.openGraphDescription?.length ?? 0}/300</span><textarea maxLength={300} rows={3} value={active.openGraphDescription ?? ""} onChange={(e) => update("openGraphDescription", e.target.value)} /></label><label>Open Graph görseli<select value={active.openGraphImageMediaId ?? ""} onChange={(e) => update("openGraphImageMediaId", e.target.value || null)}><option value="">Görsel seçilmedi</option>{media.filter((x) => x.assetType === "Image" && x.status === "Active").map((x) => <option value={x.id} key={x.id}>{x.translations.find((t) => t.languageCode === "tr")?.title ?? x.originalFileName ?? x.id}</option>)}</select></label>
      <div className={styles.checks}><label><input type="checkbox" checked={active.noIndex} onChange={(e) => update("noIndex", e.target.checked)} />Noindex</label><label><input type="checkbox" checked={active.noFollow} onChange={(e) => update("noFollow", e.target.checked)} />Nofollow</label></div></div>
      <div className={styles.previews}><article><small>ARAMA MOTORU ÖNİZLEMESİ</small><h3>{active.metaTitle || active.contentName}</h3><a>{active.canonicalUrl || `/${active.slug}`}</a><p>{active.metaDescription || "Meta description eksik."}</p></article><article><small>SOSYAL PAYLAŞIM ÖNİZLEMESİ</small>{active.openGraphImageUrl && <img src={active.openGraphImageUrl} alt="" />}<h3>{active.openGraphTitle || active.metaTitle || active.contentName}</h3><p>{active.openGraphDescription || active.metaDescription || "Open Graph açıklaması eksik."}</p></article></div>
      <footer><span>{dirty ? "Kaydedilmemiş değişiklik var" : "Tüm değişiklikler kayıtlı"}</span><button disabled={busy}>{busy ? "Kaydediliyor…" : "SEO’yu kaydet"}</button></footer>
    </form></div>}
  </main>;
}
