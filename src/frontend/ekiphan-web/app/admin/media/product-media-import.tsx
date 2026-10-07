"use client";

import { useState } from "react";
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
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<PreviewResult | null>(null);
  const [validation, setValidation] = useState<ValidationResult | null>(null);
  const [result, setResult] = useState<ExecutionResult | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function previewZip() {
    if (!file) return;

    setBusy(true);
    setError(null);
    setPreview(null);
    setValidation(null);
    setResult(null);

    try {
      const form = new FormData();
      form.set("file", file);

      const response = await fetch(
        "/api/admin/product-media-import/preview",
        {
          method: "POST",
          body: form
        }
      );

      if (!response.ok) {
        throw new Error(await readError(response));
      }

      setPreview((await response.json()) as PreviewResult);
    } catch (reason) {
      setError(
        reason instanceof Error ? reason.message : "ZIP önizlenemedi."
      );
    } finally {
      setBusy(false);
    }
  }

  async function validateImport() {
    if (!preview) return;

    setBusy(true);
    setError(null);

    try {
      const response = await fetch(
        "/api/admin/product-media-import/validate",
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            uploadToken: preview.uploadToken,
            manualMappings: null,
            importOptions: {
              skipUnmatchedFiles: true,
              replaceExistingPrimaryImage: false,
              skipDuplicateContent: true
            }
          })
        }
      );

      if (!response.ok) {
        throw new Error(await readError(response));
      }

      setValidation((await response.json()) as ValidationResult);
    } catch (reason) {
      setError(
        reason instanceof Error ? reason.message : "Eşleştirme doğrulanamadı."
      );
    } finally {
      setBusy(false);
    }
  }

  async function executeImport() {
    if (!validation) return;

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
      const response = await fetch(
        "/api/admin/product-media-import/execute",
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            validationToken: validation.validationToken
          })
        }
      );

      if (!response.ok) {
        throw new Error(await readError(response));
      }

      setResult((await response.json()) as ExecutionResult);
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
      <h2>Toplu ürün görsel importu</h2>

      <p>
        ZIP içindeki görseller dosya adındaki SKU üzerinden ürünlerle
        otomatik eşleştirilir.
      </p>
      <p>WebP önerilir · JPG, JPEG, PNG desteklenir · Görsel başına maksimum 4 MB.</p>

      {error && (
        <div className={styles.error} role="alert">
          {error}
        </div>
      )}

      <label>
        ZIP dosyası
        <input
          type="file"
          accept=".zip,application/zip"
          disabled={busy}
          onChange={(event) => {
            setFile(event.target.files?.[0] ?? null);
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
        {busy ? "İşleniyor…" : "ZIP'i önizle"}
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
