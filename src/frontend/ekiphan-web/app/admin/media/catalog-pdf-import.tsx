"use client";

import { useMemo, useState } from "react";
import { uploadZip, uploadSingle, processZip, type ZipProgress } from "../../../lib/resumable-zip-upload";
import { ZipUploadSessions } from "./zip-upload-sessions";
import styles from "../catalog/products/products.module.css";

type PreviewFile = {
  entryPath: string;
  fileName: string;
  length: number;
  catalogSlug: string | null;
  suggestedTitle: string;
  status: "Matched" | "Unmatched" | "Rejected" | "Ignored";
  error: string | null;
};

type PreviewResult = {
  pdfCount: number;
  ignoredCount: number;
  files: PreviewFile[];
};

type ExecutionResult = {
  uploaded: number;
  created: number;
  skipped: number;
  failed: number;
};

async function readError(response: Response): Promise<string> {
  const responseText = await response.text();

  try {
    const body = JSON.parse(responseText) as {
      detail?: string;
      title?: string;
      message?: string;
    };
    const detail = body.detail ?? body.message ?? body.title;
    if (detail) return `HTTP ${response.status}: ${detail}`;
  } catch {
    // Some upstream and development-server errors are plain text or HTML.
  }

  const plainText = responseText.replace(/<[^>]*>/g, " ").replace(/\s+/g, " ").trim();
  return plainText
    ? `HTTP ${response.status}: ${plainText}`
    : `HTTP ${response.status}: Sunucu hata ayrıntısı göndermedi.`;
}

export function CatalogPdfImport() {
  const [uploadId, setUploadId] = useState<string | null>(null);
  const [progress, setProgress] = useState<ZipProgress | null>(null);
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<PreviewResult | null>(null);
  const [result, setResult] = useState<ExecutionResult | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [titles, setTitles] = useState<Record<string, string>>({});

  const summary = useMemo(() => {
    const files = preview?.files ?? [];
    const count = (status: PreviewFile["status"]) =>
      files.filter((item) => item.status === status).length;

    return {
      pdfCount: preview?.pdfCount ?? 0,
      matched: count("Matched"),
      unmatched: count("Unmatched"),
      duplicates: 0,
      ignored: preview?.ignoredCount ?? count("Ignored"),
      invalidPdf: files.filter(
        (item) => item.status === "Rejected" && item.error?.includes("PDF")
      ).length,
      unsafePath: files.filter(
        (item) => item.status === "Rejected" && item.error?.includes("path")
      ).length,
      sizeErrors: files.filter(
        (item) => item.status === "Rejected" && item.error?.includes("size")
      ).length
    };
  }, [preview]);

  const canExecute = preview !== null &&
    summary.pdfCount > 0 && summary.matched === summary.pdfCount &&
    summary.unmatched === 0 &&
    summary.duplicates === 0 &&
    summary.invalidPdf === 0 &&
    summary.unsafePath === 0 &&
    summary.sizeErrors === 0;

  async function previewZip() {
    if (!file) return;

    setBusy(true);
    setError(null);
    setPreview(null);
    setResult(null);

    try {
      const uploaded = await uploadZip<PreviewResult>(file, "catalog", setProgress, setUploadId);
      setUploadId(uploaded.id);
      const next = uploaded.preview;
      setPreview(next);
      setTitles(Object.fromEntries(next.files.map((item) => [item.entryPath, item.suggestedTitle])));
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "Önizleme isteği için beklenmeyen bir hata oluştu."
      );
    } finally {
      setBusy(false);
    }
  }

  async function executeImport() {
    if (!file || !uploadId || !canExecute) return;

    setBusy(true);
    setError(null);
    setResult(null);

    try {
      setResult(await processZip<ExecutionResult>(uploadId, "execute", titles, setProgress));
    } catch (reason) {
      setError(
        reason instanceof Error
          ? reason.message
          : "İçe aktarma isteği için beklenmeyen bir hata oluştu."
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className={styles.editor} aria-labelledby="catalog-pdf-import-title">
      {progress && <div role="status" aria-live="polite"><p>{progress.message}{progress.phase === "transfer" ? `: %${progress.percent}` : ""}</p><progress max={100} value={progress.phase === "processing" ? undefined : progress.percent} /></div>}
      <h2 id="catalog-pdf-import-title">Toplu Katalog PDF İçe Aktarma</h2>
      <ZipUploadSessions kind="catalog" disabled={busy}
        refreshKey={`${busy}:${file?.name}:${uploadId}:${error}`}
        onClosed={id => { setError(null); if (id === uploadId) { setUploadId(null); setPreview(null); setResult(null); setProgress(null); setTitles({}); } }} />
      <p>
        ZIP/RAR içindeki geçerli PDF katalogları önizlenir. Maksimum arşiv 1,5 GB,
        açılmış içerik 2 GB ve her PDF 500 MB olabilir.
      </p>

      {error && <div className={styles.error} role="alert">{error}</div>}

      <label>
        Katalog ZIP/RAR dosyası
        <input
          type="file"
          accept=".zip,.rar,application/zip,application/vnd.rar,application/x-rar-compressed"
          disabled={busy}
          onChange={(event) => {
            setFile(event.target.files?.[0] ?? null);
            setUploadId(null);
            setProgress(null);
            setPreview(null);
            setResult(null);
            setError(null);
          }}
        />
      </label>

      <button type="button" disabled={busy || !file} onClick={() => void previewZip()}>
        {busy ? "Önizleniyor…" : "Önizle"}
      </button>

      {preview && (
        <div aria-live="polite">
          <h3>Önizleme sonucu</h3>
          <p>
            PDF Count: <strong>{summary.pdfCount}</strong> · Matched: <strong>{summary.matched}</strong> ·
            Unmatched: <strong>{summary.unmatched}</strong> ·
            Duplicates: <strong>{summary.duplicates}</strong>
          </p>
          {preview.files.filter((item) => item.status === "Matched").map((item) =>
            <label key={item.entryPath}>Katalog başlığı · {item.fileName}
              <input maxLength={250} value={titles[item.entryPath] ?? ""}
                onChange={(event) => setTitles((current) => ({ ...current, [item.entryPath]: event.target.value }))} />
              <small>{(item.length / 1024 / 1024).toFixed(1)} MB · Kapak: PDF ilk sayfa</small>
            </label>)}
          <p>
            Ignored: <strong>{summary.ignored}</strong> · Invalid PDF: <strong>{summary.invalidPdf}</strong> ·
            Unsafe Path: <strong>{summary.unsafePath}</strong> · Size Errors: <strong>{summary.sizeErrors}</strong>
          </p>
          <button type="button" disabled={busy || !canExecute} onClick={() => void executeImport()}>
            {busy ? "İçe aktarılıyor…" : "İçe Aktar"}
          </button>
        </div>
      )}

      {result && (
        <div className={styles.success} role="status">
          Uploaded: {result.uploaded} · Created: {result.created} · Updated/Skipped: {result.skipped} · Failed: {result.failed}
        </div>
      )}
    </section>
  );
}

export function SingleCatalogPdfUpload({ onUploaded }: { onUploaded: () => Promise<void> }) {
  const [uploadId, setUploadId] = useState<string | null>(null);
  const [progress, setProgress] = useState<ZipProgress | null>(null);
  const [file, setFile] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);


  async function upload() {
    if (!file) return;
    if (!/\.pdf$/i.test(file.name) || (file.type && file.type !== "application/pdf")) {
      setError("Yalnız PDF dosyası yüklenebilir."); return;
    }
    if (file.size <= 0 || file.size > 500 * 1024 * 1024) {
      setError("PDF en fazla 500 MB olabilir."); return;
    }
    setBusy(true); setError(null); setMessage(null);
    try {
      const result = await uploadSingle<{ warning?: string | null }>(file, "pdf",
        {}, setProgress, setUploadId);
      setMessage(result.warning ?? "Katalog PDF ve WebP kapağı yüklendi.");
      setFile(null); await onUploaded();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Katalog PDF yüklenemedi.");
    } finally { setBusy(false); }
  }

  return <section className={styles.editor} aria-labelledby="single-catalog-title">
    <h2 id="single-catalog-title">Tek Katalog PDF Yükle</h2>
    <ZipUploadSessions kind="pdf" disabled={busy} refreshKey={`${busy}:${uploadId}:${error}`}
      onClosed={id => { if (id === uploadId) { setUploadId(null); setProgress(null); } }} />
    {progress && <div role="status" aria-live="polite"><p>{progress.message}{progress.phase === "transfer" ? `: %${progress.percent}` : ""}</p><progress max={100} value={progress.phase === "processing" ? undefined : progress.percent} /></div>}
    {error && <div className={styles.error} role="alert">{error}</div>}
    {message && <div className={styles.success} role="status">{message}</div>}
    <label>PDF dosyası seç<input type="file" accept=".pdf,application/pdf" disabled={busy}
      onChange={event => { const selected = event.target.files?.[0] ?? null; setFile(selected);
        setError(null); setMessage(null); }} /></label>
    <button type="button" disabled={busy || !file} onClick={() => void upload()}>
      {busy ? "Yükleniyor…" : "Yükle"}
    </button>
    <small>Yalnız PDF · Maksimum 500 MB · İlk sayfadan WebP kapak üretilir.</small>
  </section>;
}
