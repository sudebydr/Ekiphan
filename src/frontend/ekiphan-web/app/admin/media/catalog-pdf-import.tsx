"use client";

import { useMemo, useState } from "react";
import styles from "../catalog/products/products.module.css";

type PreviewFile = {
  entryPath: string;
  fileName: string;
  length: number;
  catalogSlug: string | null;
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

const manifestCount = 12;

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
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<PreviewResult | null>(null);
  const [result, setResult] = useState<ExecutionResult | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const summary = useMemo(() => {
    const files = preview?.files ?? [];
    const count = (status: PreviewFile["status"]) =>
      files.filter((item) => item.status === status).length;

    return {
      pdfCount: preview?.pdfCount ?? 0,
      matched: count("Matched"),
      unmatched: count("Unmatched"),
      missing: preview ? Math.max(0, manifestCount - count("Matched")) : 0,
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
    summary.pdfCount === manifestCount &&
    summary.matched === manifestCount &&
    summary.unmatched === 0 &&
    summary.missing === 0 &&
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
      const form = new FormData();
      form.set("file", file);
      const response = await fetch("/api/admin/catalog-pdf-import/preview", {
        method: "POST",
        body: form
      });

      if (!response.ok) throw new Error(await readError(response));
      setPreview((await response.json()) as PreviewResult);
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
    if (!file || !canExecute) return;

    setBusy(true);
    setError(null);
    setResult(null);

    try {
      const form = new FormData();
      form.set("file", file);
      form.set("confirmed", "true");
      const response = await fetch("/api/admin/catalog-pdf-import/execute", {
        method: "POST",
        body: form
      });

      if (!response.ok) throw new Error(await readError(response));
      setResult((await response.json()) as ExecutionResult);
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
      <h2 id="catalog-pdf-import-title">Katalog PDF Import</h2>
      <p>
        ZIP yalnızca doğrulanır ve 12 katalog kartıyla eşleştirilir. Bu adım
        S3'e veya veritabanına yazmaz.
      </p>

      {error && <div className={styles.error} role="alert">{error}</div>}

      <label>
        Katalog ZIP dosyası
        <input
          type="file"
          accept=".zip,application/zip"
          disabled={busy}
          onChange={(event) => {
            setFile(event.target.files?.[0] ?? null);
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
            Unmatched: <strong>{summary.unmatched}</strong> · Missing: <strong>{summary.missing}</strong> ·
            Duplicates: <strong>{summary.duplicates}</strong>
          </p>
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
