"use client";
import { uploadZip, processZip, type ZipProgress } from "../../../lib/resumable-zip-upload";

import { useState } from "react";
import { ZipUploadSessions } from "./zip-upload-sessions";
import styles from "../catalog/products/products.module.css";

type PreviewFile = {
  temporaryFileId: string;
  originalFileName: string;
  extractedSku?: string | null;
  detectedPosition: string | number;
  suggestedSortOrder: number;
  suggestedIsPrimary: boolean;
  status: string | number;
  errorMessage?: string | null;
};

type PreviewResult = {
  uploadToken: string;
  fileName: string;
  totalEntryCount: number;
  supportedImageCount: number;
  ignoredFileCount: number;
  invalidFileCount: number;
  duplicateFileCount: number;
  skippedEntryCount: number;
  extractedSkuCount: number;
  files: PreviewFile[];
  summary: string;
};

type ValidationFile = {
  temporaryFileId: string;
  originalFileName: string;
  extractedSku?: string | null;
  matchedProductId?: string | null;
  matchedProductSku?: string | null;
  matchedProductName?: string | null;
  sortOrder: number;
  isPrimary: boolean;
  status: string | number;
  errors?: { code: string; message: string }[];
  warnings?: { code: string; message: string }[];
};

type ValidationResult = {
  validationToken: string;
  totalFiles: number;
  matchedFiles: number;
  unmatchedFiles: number;
  duplicateFiles: number;
  invalidFiles: number;
  ignoredFiles: number;
  productsWithImages: number;
  productsWithoutPrimaryImage: number;
  conflicts: number;
  files: ValidationFile[];
  summary: string;
};

type ExecutionResult = {
  batchId: string;
  status: string | number;
  importedFiles: number;
  skippedFiles: number;
  errorFiles: number;
  errors: { code: string; field?: string | null; message: string }[];
};

const reasonText: Record<string, string> = {
  PRODUCT_NOT_FOUND: "Ürün SKU'su sistemde bulunamadı",
  SKU_RESOLUTION_FAILED: "Dosya adından güvenli SKU çıkarılamadı",
  AMBIGUOUS_SKU: "Birden fazla ürünle eşleşiyor",
  INVALID_IMAGE: "Geçersiz görsel",
  EXTENSION_CONTENT_MISMATCH: "Dosya uzantısı ve içeriği uyuşmuyor",
  DUPLICATE_MEDIA: "Tekrar görsel"
};

function validationReason(item: ValidationFile) {
  const code = item.errors?.[0]?.code ?? item.warnings?.[0]?.code;
  return code ? reasonText[code] ?? code : null;
}

async function readError(response: Response) {
  try {
    const data = await response.json();
    return data.detail ?? data.title ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function ProductMediaImport() {
  const [uploadId, setUploadId] = useState<string | null>(null);
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<PreviewResult | null>(null);
  const [validation, setValidation] = useState<ValidationResult | null>(null);
  const [result, setResult] = useState<ExecutionResult | null>(null);
  const [busy, setBusy] = useState(false);
  const [progress, setProgress] = useState<ZipProgress | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function previewZip() {
    if (!file) return;

    setBusy(true);
    setError(null);
    setPreview(null);
    setValidation(null);
    setResult(null);

    try {
      const uploaded = await uploadZip<PreviewResult>(file, "product", setProgress, setUploadId);
      setUploadId(uploaded.id);
      setPreview(uploaded.preview);
    } catch (reason) {
      setError(
        reason instanceof Error ? reason.message : "ZIP önizlenemedi."
      );
    } finally {
      setBusy(false);
    }
  }

  async function validateImport() {
    if (!preview || !uploadId) return;

    setBusy(true);
    setError(null);

    try {
      setValidation(await processZip<ValidationResult>(uploadId, "validate", undefined, setProgress));
    } catch (reason) {
      setError(
        reason instanceof Error ? reason.message : "Eşleştirme doğrulanamadı."
      );
    } finally {
      setBusy(false);
    }
  }

  async function executeImport() {
    if (!validation || !uploadId) return;

    if (
      !window.confirm(
        `${validation.matchedFiles} eşleşen görsel gerçekten içe aktarılsın mı?`
      )
    ) {
      return;
    }

    setBusy(true);
    setError(null);

    try {
      setResult(await processZip<ExecutionResult>(uploadId, "execute", undefined, setProgress, validation.validationToken));
    } catch (reason) {
      setError(
        reason instanceof Error ? reason.message : "Görseller içe aktarılamadı."
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className={styles.editor}>
      {progress && <div role="status" aria-live="polite"><p>{progress.message}{progress.phase === "transfer" ? `: %${progress.percent}` : ""}</p><progress max={100} value={progress.phase === "processing" ? undefined : progress.percent} /></div>}
      <h2>Toplu Ürün Görsel İçe Aktarma</h2>
      <ZipUploadSessions kind="product" disabled={busy}
        refreshKey={`${busy}:${file?.name}:${uploadId}:${error}`}
        onClosed={id => { setError(null); if (id === uploadId) { setUploadId(null); setPreview(null); setResult(null); setProgress(null); setValidation(null); } }} />

      <p>
        ZIP/RAR içindeki görseller dosya adındaki SKU üzerinden ürünlerle
        otomatik eşleştirilir.
      </p>
      <p>WebP önerilir · JPG, JPEG, PNG desteklenir · Görsel başına maksimum 4 MB.</p>

      {error && (
        <div className={styles.error} role="alert">
          {error}
        </div>
      )}

      <label>
        ZIP/RAR dosyası
        <input
          type="file"
          accept=".zip,.rar,application/zip,application/vnd.rar,application/x-rar-compressed"
          disabled={busy}
          onChange={(event) => {
            setFile(event.target.files?.[0] ?? null);
            setUploadId(null);
            setProgress(null);
            setPreview(null);
            setValidation(null);
            setResult(null);
            setError(null);
          }}
        />
      </label>

      <button
        type="button"
        disabled={busy || !file}
        onClick={() => void previewZip()}
      >
        {busy ? "İşleniyor…" : "Arşivi önizle"}
      </button>

      {preview && (
        <div>
          <h3>Önizleme</h3>
          <p>
            Toplam: <strong>{preview.totalEntryCount}</strong> ·
            Desteklenen görsel: <strong>{preview.supportedImageCount}</strong> ·
            SKU bulunan: <strong>{preview.extractedSkuCount}</strong> ·
            Geçersiz: <strong>{preview.invalidFileCount}</strong> ·
            Tekrar: <strong>{preview.duplicateFileCount}</strong>
          </p>

          <p>Atlanan arşiv girdisi: <strong>{preview.skippedEntryCount}</strong></p>

          <button
            type="button"
            disabled={busy || preview.supportedImageCount === 0}
            onClick={() => void validateImport()}
          >
            SKU eşleşmelerini doğrula
          </button>
        </div>
      )}

      {validation && (
        <div>
          <h3>Eşleştirme sonucu</h3>

          <p>
            Eşleşen: <strong>{validation.matchedFiles}</strong> ·
            Eşleşmeyen: <strong>{validation.unmatchedFiles}</strong> ·
            Tekrar: <strong>{validation.duplicateFiles}</strong> ·
            Geçersiz: <strong>{validation.invalidFiles}</strong> ·
            Çakışma: <strong>{validation.conflicts}</strong>
          </p>

          {validation.files
            .filter((item) => item.status !== 0)
            .slice(0, 20)
            .map((item) => (
              <p key={item.temporaryFileId} title={validationReason(item) ?? undefined}>
                {validationReason(item) ? `${validationReason(item)} — ` : null}
                {item.originalFileName} →{" "}
                {item.matchedProductSku ?? "EŞLEŞMEDİ"}
              </p>
            ))}

          <button
            type="button"
            disabled={
              busy ||
              validation.matchedFiles === 0
            }
            onClick={() => void executeImport()}
          >
            Görselleri içe aktar
          </button>
        </div>
      )}

      {result && (
        <div className={styles.success}>
          Import tamamlandı. Eklenen: {result.importedFiles} ·
          Atlanan: {result.skippedFiles} ·
          Hatalı: {result.errorFiles}
          <br />
          Batch: {result.batchId}
          {result.errors.length > 0 && (
            <><br />İlk hata: {result.errors[0].message}</>
          )}
        </div>
      )}
    </div>
  );
}
