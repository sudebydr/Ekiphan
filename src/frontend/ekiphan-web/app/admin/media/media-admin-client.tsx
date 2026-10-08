"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import type {
  AdminMediaAsset,
  AdminMediaLibrary
} from "../../../lib/admin-media-types";
import type { ProblemDetails } from "../../../lib/admin-product-relation-types";
import styles from "../catalog/products/products.module.css";
import { CatalogPdfImport, SingleCatalogPdfUpload } from "./catalog-pdf-import";
import { ProductMediaImport } from "./product-media-import";

type Target = { id: string; name: string };

async function readError(response: Response) {
  try {
    const problem = (await response.json()) as ProblemDetails;
    return problem.detail ?? problem.title ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function MediaAdminClient() {
  const [library, setLibrary] = useState<AdminMediaLibrary>({
    assets: [], assignments: [], products: [], brands: [], categories: [],
    page: 1, pageSize: 40, totalCount: 0
  });
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [title, setTitle] = useState("");
  const [altText, setAltText] = useState("");
  const [description, setDescription] = useState("");
  const [englishTitle, setEnglishTitle] = useState("");
  const [englishAlt, setEnglishAlt] = useState("");
  const [englishDescription, setEnglishDescription] = useState("");
  const [externalUrl, setExternalUrl] = useState("");
  const [files, setFiles] = useState<File[]>([]);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [assetFilter, setAssetFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [page, setPage] = useState(1);
  const [targetType, setTargetType] = useState<"product" | "brand" | "category">("product");
  const [targetId, setTargetId] = useState("");
  const [mediaAssetId, setMediaAssetId] = useState("");
  const [role, setRole] = useState("GalleryImage");
  const [isDefault, setIsDefault] = useState(false);
  const [sortOrder, setSortOrder] = useState(0);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    const query = new URLSearchParams({ page: String(page), pageSize: "40" });
    if (search) query.set("search", search);
    if (assetFilter) query.set("assetType", assetFilter);
    if (statusFilter) query.set("status", statusFilter);
    setLoading(true);
    try {
      const response = await fetch(`/api/admin/media-library?${query}`, {
        cache: "no-store"
      });
      if (!response.ok) throw new Error(await readError(response));
      setLibrary((await response.json()) as AdminMediaLibrary);
    } finally {
      setLoading(false);
    }
  }, [assetFilter, page, search, statusFilter]);

  useEffect(() => {
    void load().catch((reason: unknown) =>
      setError(reason instanceof Error ? reason.message : "Medya verileri alınamadı."));
  }, [load]);

  const selected = library.assets.find((item) => item.id === selectedId);
  const selectedUsages = library.assignments.filter(
    (item) => item.mediaAssetId === selectedId
  );
  const targets: Target[] = (
    targetType === "product"
      ? library.products
      : targetType === "brand"
        ? library.brands
        : library.categories
  ).map((item) => ({
    id: item.id,
    name: item.code ? `${item.name} · ${item.code}` : item.name
  }));
  const roles = targetType === "product"
    ? ["GalleryImage", "PdfCatalog", "Document", "Video"]
    : targetType === "brand"
      ? ["Logo", "PdfCatalog"]
      : ["Image", "Icon"];

  const compatibleAssets = useMemo(() => library.assets.filter((item) => {
    if (item.status !== "Active") return false;
    if (targetType === "category") return item.assetType === "Image";
    if (targetType === "brand") return role === "Logo"
      ? item.assetType === "Image"
      : item.assetType === "Pdf";
    return role === "GalleryImage" ? item.assetType === "Image"
      : role === "PdfCatalog" ? item.assetType === "Pdf"
        : role === "Video" ? item.assetType === "ExternalVideo"
          : item.assetType === "Document" || item.assetType === "Pdf";
  }), [library.assets, role, targetType]);

  function chooseAsset(asset: AdminMediaAsset) {
    setSelectedId(asset.id);
    const tr = asset.translations.find((item) => item.languageCode === "tr");
    const en = asset.translations.find((item) => item.languageCode === "en");
    setTitle(tr?.title ?? ""); setAltText(tr?.altText ?? "");
    setDescription(tr?.description ?? "");
    setEnglishTitle(en?.title ?? ""); setEnglishAlt(en?.altText ?? "");
    setEnglishDescription(en?.description ?? "");
    setMediaAssetId(asset.id);
  }

  function translations(image: boolean) {
    return [
      { languageCode: "tr", title, altText: image ? altText : null, description },
      ...(englishTitle
        ? [{ languageCode: "en", title: englishTitle,
          altText: image ? englishAlt : null, description: englishDescription }]
        : [])
    ];
  }

  async function jsonRequest(url: string, method: "POST" | "PUT" | "DELETE", body?: object) {
    setBusy(true); setError(null); setMessage(null);
    try {
      const response = await fetch(url, {
        method,
        headers: body ? { "Content-Type": "application/json" } : undefined,
        body: body ? JSON.stringify(body) : undefined
      });
      if (!response.ok) throw new Error(await readError(response));
      await load();
      return true;
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "İşlem tamamlanamadı.");
      return false;
    } finally { setBusy(false); }
  }

  async function upload(event: FormEvent) {
    event.preventDefault(); if (files.length === 0) return;
    const maximum = 4 * 1024 * 1024;
    if (files.some((file) => file.size > maximum)) {
      setError("Dosya 4 MB sınırını aşıyor.");
      return;
    }
    setBusy(true); setError(null); setMessage(null);
    try {
      for (const file of files) {
        const form = new FormData();
        const fallbackTitle = file.name.replace(/\.[^.]+$/, "");
        form.set("file", file); form.set("assetType", "Image");
        form.set("languageCode", "tr");
        form.set("title", files.length === 1 ? title : fallbackTitle);
        form.set("altText", altText || fallbackTitle);
        if (files.length === 1 && description) form.set("description", description);
        const response = await fetch("/api/admin/media", { method: "POST", body: form });
        if (!response.ok) throw new Error(await readError(response));
      }
      setMessage(`${files.length} dosya medyaya eklendi.`); setFiles([]); await load();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Dosya yüklenemedi.");
    } finally { setBusy(false); }
  }

  async function createVideo(event: FormEvent) {
    event.preventDefault();
    const ok = await jsonRequest("/api/admin/media-library/external-video", "POST", {
      externalUrl, translations: translations(false)
    });
    if (ok) { setMessage("Harici video oluşturuldu."); setExternalUrl(""); }
  }

  async function updateAsset(archive = false) {
    if (!selected) return;
    if (archive && selectedUsages.length > 0) {
      setError("Kullanımdaki medya arşivlenemez. Önce tüm atamaları kaldırın.");
      return;
    }
    if (archive && !window.confirm("Bu medya güvenli biçimde arşivlensin mi?")) return;
    const ok = await jsonRequest(`/api/admin/media-library/assets/${selected.id}`, "PUT", {
      archive, translations: translations(selected.assetType === "Image")
    });
    if (ok) setMessage(archive ? "Medya arşivlendi." : "Medya metinleri güncellendi.");
  }

  async function deletePdf(asset: AdminMediaAsset) {
    if (!window.confirm("Bu PDF’yi silmek istediğinize emin misiniz?")) return;
    const ok = await jsonRequest(
      `/api/admin/media-library/assets/${asset.id}`,
      "DELETE"
    );
    if (ok) {
      if (selectedId === asset.id) setSelectedId(null);
      setMessage("PDF silindi.");
    }
  }

  async function togglePdfStatus(asset: AdminMediaAsset) {
    const makeActive = asset.status !== "Active";
    const ok = await jsonRequest(
      `/api/admin/media-library/assets/${asset.id}/status`,
      "PUT",
      { active: makeActive }
    );
    if (ok) setMessage(`PDF ${makeActive ? "aktif" : "pasif"} yapıldı.`);
  }

  async function removeUsage(
    usage: AdminMediaLibrary["assignments"][number]
  ) {
    if (!["product", "brand", "category"].includes(usage.targetType)) return;
    const ok = await jsonRequest(
      `/api/admin/media-library/assignments/${usage.targetType}/${usage.targetId}/${usage.mediaAssetId}/${usage.role}`,
      "DELETE"
    );
    if (ok) setMessage("Medya ataması kaldırıldı.");
  }

  function usageLabel(usage: AdminMediaLibrary["assignments"][number]) {
    const source = usage.targetType === "product" ? library.products
      : usage.targetType === "brand" ? library.brands
        : usage.targetType === "category" ? library.categories : [];
    const target = source.find((item) => item.id === usage.targetId);
    return `${target?.name ?? usage.targetType} · ${usage.role}`;
  }

  async function saveAssignment(event: FormEvent) {
    event.preventDefault();
    const ok = await jsonRequest("/api/admin/media-library/assignments", "PUT", {
      targetType, targetId, mediaAssetId, role,
      isDefault: targetType === "product" && role === "GalleryImage" && isDefault,
      sortOrder
    });
    if (ok) setMessage("Medya ataması kaydedildi.");
  }

  const assetName = (asset: AdminMediaAsset) => {
    const translatedTitle = asset.translations
      .find((item) => item.languageCode === "tr")?.title.trim();
    return translatedTitle || asset.originalFileName || asset.assetType;
  };

  const formatCreatedAt = (value: string) => new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));

  return <main className={styles.page}>
    <header className={styles.header}><div><p className={styles.eyebrow}>EKİPHAN · ADMIN</p><h1>Medya yönetimi</h1>
      <p className={styles.lead}>Dosyaları, harici videoları ve katalog görsel/doküman atamalarını yönetin.</p></div>
      <nav className={styles.nav}><a href="/admin">Dashboard</a><a href="/admin/catalog/products">Ürünler</a><a href="/admin/catalog/brands">Markalar</a><a href="/admin/catalog/categories">Kategoriler</a><a href="/admin/catalog/dictionaries">Etiketler ve birimler</a><a href="/">Siteye dön</a></nav>
    </header>
    {error && <div className={styles.error} role="alert">{error}</div>}
    {message && <div className={styles.success} role="status">{message}</div>}
    <div className={styles.layout}>
      <section className={styles.panel}><h2>Medya kütüphanesi</h2>
        <form className={styles.search} onSubmit={(event) => {
          event.preventDefault(); setPage(1); setSearch(searchInput.trim());
        }}><label htmlFor="media-search">Dosya veya başlık ara</label>
          <div><input id="media-search" maxLength={100} value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)} />
            <button disabled={loading}>Ara</button></div>
          <div className={styles.filters}>
            <label>Tür<select value={assetFilter} onChange={(event) => {
              setAssetFilter(event.target.value); setPage(1);
            }}><option value="">Tümü</option><option>Image</option><option>Pdf</option>
              <option>Document</option><option>ExternalVideo</option></select></label>
            <label>Durum<select value={statusFilter} onChange={(event) => {
              setStatusFilter(event.target.value); setPage(1);
            }}><option value="">Tümü</option><option>Active</option>
              <option>Archived</option></select></label>
          </div>
        </form>
        <div className={styles.productList}>
          {loading && <p className={styles.listState}>Medya yükleniyor…</p>}
          {!loading && library.assets.length === 0 &&
            <p className={styles.listState}>Filtreye uygun medya bulunamadı.</p>}
          {!loading && library.assets.map((asset) => asset.assetType === "Pdf" ?
            <article key={asset.id}
              className={`${styles.pdfCard} ${selectedId === asset.id ? styles.pdfCardSelected : ""}`}>
              <button type="button" className={styles.pdfCardMain}
                onClick={() => chooseAsset(asset)}>
                <span className={styles.pdfBadge}>PDF</span>
                <strong>{assetName(asset)}</strong>
                <span className={styles.pdfFileName}>{asset.originalFileName ?? "Dosya adı belirtilmemiş"}</span>
                <span className={styles.pdfMeta}>
                  Tür: PDF · Durum: {asset.status === "Active" ? "Aktif" : "Pasif"}
                </span>
                <time className={styles.pdfDate} dateTime={asset.createdAt}>
                  Yüklenme: {formatCreatedAt(asset.createdAt)}
                </time>
              </button>
              <div className={styles.pdfActions}>
                <button type="button" className={styles.pdfStatus} disabled={busy}
                  onClick={() => void togglePdfStatus(asset)}>
                  {asset.status === "Active" ? "Pasif yap" : "Aktif yap"}
                </button>
                <button type="button" className={styles.pdfDelete} disabled={busy}
                  onClick={() => void deletePdf(asset)}>Sil</button>
              </div>
            </article>
            : <button type="button" key={asset.id} onClick={() => chooseAsset(asset)}
                className={selectedId === asset.id ? styles.selected : styles.product}>
                <strong>{assetName(asset)}</strong><span>{asset.assetType}</span><small>{asset.status}</small>
              </button>)}</div>
        {library.totalCount > library.pageSize && <div className={styles.pagination}>
          <button type="button" disabled={page <= 1 || loading}
            onClick={() => setPage((value) => value - 1)}>Önceki</button>
          <span>{library.totalCount} kayıt · Sayfa {library.page}</span>
          <button type="button" disabled={page * library.pageSize >= library.totalCount || loading}
            onClick={() => setPage((value) => value + 1)}>Sonraki</button>
        </div>}
        {selected && <form className={styles.editor} onSubmit={(e) => { e.preventDefault(); void updateAsset(); }}>
          <h3>Medya metinleri</h3>
          <label>Türkçe başlık<input required maxLength={250} value={title} onChange={(e) => setTitle(e.target.value)} /></label>
          {selected.assetType === "Image" && <label>Türkçe alt metin<input required maxLength={500} value={altText} onChange={(e) => setAltText(e.target.value)} /></label>}
          <label>Türkçe açıklama<textarea maxLength={2000} value={description} onChange={(e) => setDescription(e.target.value)} /></label>
          <label>İngilizce başlık<input maxLength={250} value={englishTitle} onChange={(e) => setEnglishTitle(e.target.value)} /></label>
          {selected.assetType === "Image" && englishTitle && <label>İngilizce alt metin<input required maxLength={500} value={englishAlt} onChange={(e) => setEnglishAlt(e.target.value)} /></label>}
          {englishTitle && <label>İngilizce açıklama<textarea maxLength={2000} value={englishDescription} onChange={(e) => setEnglishDescription(e.target.value)} /></label>}
          {selected.assetType === "Image" && selected.url &&
            <img className={styles.mediaPreview} src={selected.url}
              alt={altText} width={480} height={300} />}
          {selected.url && <a href={selected.url} target="_blank" rel="noreferrer">Medyayı aç</a>}
          <div><strong>Kullanıldığı yerler ({selectedUsages.length})</strong>
            {selectedUsages.length === 0 ? <p className={styles.listState}>Bu medya kullanılmıyor.</p>
              : <ul className={styles.categoryOrder}>{selectedUsages.map((usage) =>
                <li key={`${usage.targetType}-${usage.targetId}-${usage.role}`}>
                  <span>{usageLabel(usage)}</span>
                  {["product", "brand", "category"].includes(usage.targetType) &&
                    <button type="button" disabled={busy}
                      onClick={() => void removeUsage(usage)}>Kaldır</button>}
                </li>)}</ul>}
          </div>
          <div className={styles.actions}><button disabled={busy}>Güncelle</button>
            {selected.status === "Active" && <button type="button" className={styles.danger}
              disabled={busy || selectedUsages.length > 0}
              onClick={() => void updateAsset(true)}>Güvenli arşivle</button>}</div>
        </form>}
      </section>
      <section className={styles.panel}>
        <ProductMediaImport />
        <CatalogPdfImport />
        <form className={styles.editor} onSubmit={upload}><h2>Tek Ürün Görseli Yükle</h2>
          <label>Dosya<input type="file" required
            accept=".webp,.jpg,.jpeg,.png,image/webp,image/jpeg,image/png"
            onChange={(e) => setFiles(Array.from(e.target.files ?? []))} /></label>
          <label>Türkçe başlık<input required={files.length <= 1} maxLength={250} value={title} onChange={(e) => setTitle(e.target.value)} /></label>
          <label>Alt metin<input required maxLength={500} value={altText} onChange={(e) => setAltText(e.target.value)} /></label>
          <label>Açıklama<textarea maxLength={2000} value={description} onChange={(e) => setDescription(e.target.value)} /></label>
          <button disabled={busy || files.length === 0}>Güvenli yükle ({files.length})</button>
          <small>Önerilen format: WebP · Maksimum dosya boyutu: 4 MB · Ürüne aşağıdaki manuel medya atama alanından bağlayın.</small>
          <small>Tehdit tarayıcısı yapılandırılmamışsa sistem güvenlik gereği yüklemeyi reddeder.</small>
        </form>
        <SingleCatalogPdfUpload onUploaded={load} catalogs={library.assets
          .filter(asset => asset.assetType === "Pdf" && asset.url?.includes("/media/catalogs/"))
          .map(asset => ({ id: asset.id, title: assetName(asset) }))} />
        <form className={styles.editor} onSubmit={createVideo}><h2>Harici video</h2>
          <label>HTTPS video adresi<input type="url" pattern="https://.*" required value={externalUrl} onChange={(e) => setExternalUrl(e.target.value)} /></label>
          <label>Türkçe başlık<input required maxLength={250} value={title} onChange={(e) => setTitle(e.target.value)} /></label>
          <label>Açıklama<textarea maxLength={2000} value={description} onChange={(e) => setDescription(e.target.value)} /></label>
          <button disabled={busy}>Video oluştur</button>
        </form>
        <form className={styles.editor} onSubmit={saveAssignment}><h2>Manuel medya atama</h2>
          <label>Hedef türü<select value={targetType} onChange={(e) => { const next = e.target.value as typeof targetType; setTargetType(next); setTargetId(""); setRole(next === "product" ? "GalleryImage" : next === "brand" ? "Logo" : "Image"); }}>
            <option value="product">Ürün</option><option value="brand">Marka</option><option value="category">Kategori</option>
          </select></label>
          <label>Hedef<select required value={targetId} onChange={(e) => setTargetId(e.target.value)}><option value="">Seçin</option>{targets.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
          <label>Rol<select value={role} onChange={(e) => { setRole(e.target.value); setMediaAssetId(""); }}>{
            roles.map((item) => <option key={item}>{item}</option>)}</select></label>
          <label>Medya<select required value={mediaAssetId} onChange={(e) => setMediaAssetId(e.target.value)}><option value="">Seçin</option>{compatibleAssets.map((asset) => <option key={asset.id} value={asset.id}>{assetName(asset)}</option>)}</select></label>
          <label>Sıra<input type="number" value={sortOrder} onChange={(e) => setSortOrder(e.target.valueAsNumber)} /></label>
          {targetType === "product" && role === "GalleryImage" && <label className={styles.checkbox}><input type="checkbox" checked={isDefault} onChange={(e) => setIsDefault(e.target.checked)} />Varsayılan görsel</label>}
          <button disabled={busy}>Atamayı kaydet</button>
        </form>
      </section>
    </div>
  </main>;
}
