"use client";

import { FormEvent, useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import type {
  ImportIssue,
  ImportJobDetail,
  ImportJobSummary,
  ImportUploadResult,
  PagedResult,
  ProblemDetails
} from "../../../lib/import-types";
import {
  severityLabels,
  statusLabels
} from "../../../lib/import-types";
import styles from "./imports.module.css";

const pageSize = 20;

async function readError(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as ProblemDetails;
    const message = problem.detail ?? problem.title;
    return message === "An error occurred while processing your request." ||
      message === "Internal Server Error"
      ? "İsteğiniz işlenirken bir hata oluştu."
      : message ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

function formatDate(value: string | null): string {
  if (!value) return "—";
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "short",
    timeStyle: "short"
  }).format(new Date(value));
}

export function ImportAdminClient() {
  const router = useRouter();
  const deleteDialog = useRef<HTMLDialogElement>(null);
  const [deleteTarget, setDeleteTarget] = useState<ImportJobSummary | null>(null);
  const [productTotal, setProductTotal] = useState<number | null>(null);
  const [jobs, setJobs] = useState<PagedResult<ImportJobSummary> | null>(null);
  const [selectedJob, setSelectedJob] = useState<ImportJobDetail | null>(null);
  const [issues, setIssues] = useState<PagedResult<ImportIssue> | null>(null);
  const [page, setPage] = useState(1);
  const [busy, setBusy] = useState(false);
  const [loadingJobs, setLoadingJobs] = useState(true);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (deleteTarget) deleteDialog.current?.showModal();
    else deleteDialog.current?.close();
  }, [deleteTarget]);

  async function deleteImport() {
    if (!deleteTarget || busy) return;
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const response = await fetch(`/api/admin/imports/${deleteTarget.id}`, { method: "DELETE" });
      if (!response.ok) throw new Error(await readError(response));
      const result = await response.json() as { deletedProducts: number; restoredPendingRelations: number; totalProducts: number };
      setProductTotal(result.totalProducts);
      setMessage(`Import silindi: ${result.deletedProducts} ürün silindi, ${result.restoredPendingRelations} ilişki pending durumuna taşındı.`);
      if (selectedJob?.id === deleteTarget.id) { setSelectedJob(null); setIssues(null); }
      setDeleteTarget(null);
      const nextPage = jobs?.items.length === 1 && page > 1 ? page - 1 : page;
      setPage(nextPage);
      await loadJobs(nextPage);
      router.refresh();
    } catch (reason) {
      setDeleteTarget(null);
      setError(reason instanceof Error ? reason.message : "Import silinemedi.");
    } finally { setBusy(false); }
  }

  const loadJobs = useCallback(async (requestedPage: number) => {
    setError(null);
    setLoadingJobs(true);
    try {
      const response = await fetch(
        `/api/admin/imports?page=${requestedPage}&pageSize=${pageSize}`,
        { cache: "no-store" }
      );
      if (!response.ok) {
        throw new Error(await readError(response));
      }

      setJobs((await response.json()) as PagedResult<ImportJobSummary>);
    } finally {
      setLoadingJobs(false);
    }
  }, []);

  useEffect(() => {
    void loadJobs(page).catch((reason: unknown) => {
      setError(reason instanceof Error ? reason.message : "Import listesi alınamadı.");
    });
  }, [loadJobs, page]);

  async function selectJob(jobId: string) {
    setBusy(true);
    setError(null);
    try {
      const [jobResponse, issueResponse] = await Promise.all([
        fetch(`/api/admin/imports/${jobId}`, { cache: "no-store" }),
        fetch(`/api/admin/imports/${jobId}/issues?page=1&pageSize=50`, {
          cache: "no-store"
        })
      ]);
      if (!jobResponse.ok) throw new Error(await readError(jobResponse));
      if (!issueResponse.ok) throw new Error(await readError(issueResponse));
      setSelectedJob((await jobResponse.json()) as ImportJobDetail);
      setIssues((await issueResponse.json()) as PagedResult<ImportIssue>);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Import detayı alınamadı.");
    } finally {
      setBusy(false);
    }
  }

  async function upload(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setMessage(null);
    const form = event.currentTarget;
    const formData = new FormData(form);
    const dryRunControl = form.elements.namedItem("isDryRun");
    if (dryRunControl instanceof HTMLInputElement) {
      formData.set("isDryRun", String(dryRunControl.checked));
    }

    try {
      const response = await fetch("/api/admin/imports", {
        method: "POST",
        body: formData
      });

      // 422 means the file was read and staged, but row-level validation
      // failed — the response body is a normal ImportUploadResult (with
      // validRowCount/invalidRowCount), not a ProblemDetails error. Only
      // other non-OK statuses (400/409/413/415/503 etc.) are real request
      // errors and should go through readError().
      if (!response.ok && response.status !== 422) {
        throw new Error(await readError(response));
      }

      const result = (await response.json()) as ImportUploadResult;
      if (response.status === 422 || result.invalidRowCount > 0) {
        setError(
          `${result.fileName} doğrulanamadı: ${result.invalidRowCount} satırda hata var ` +
            `(${result.validRowCount} geçerli satır). Detaylar için aşağıdaki listeden bu kaydı seçip sorunlar sekmesine bakın.` +
            (result.failureReason ? ` (${result.failureReason})` : "")
        );
      } else {
        setMessage(
          `${result.fileName} işlendi: ${statusLabels[result.status]}, ${result.validRowCount} geçerli satır.`
        );
      }
      form.reset();
      await loadJobs(1);
      setPage(1);
      await selectJob(result.id);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Dosya yüklenemedi.");
    } finally {
      setBusy(false);
    }
  }

  async function publish() {
    if (!selectedJob || !window.confirm("Bu doğrulanmış importu taslak ürünlere yayınlamak istiyor musunuz?")) {
      return;
    }

    setBusy(true);
    setError(null);
    try {
      const response = await fetch(
        `/api/admin/imports/${selectedJob.id}/publish`,
        { method: "POST" }
      );
      if (!response.ok) throw new Error(await readError(response));
      setMessage("Import taslak ürünlere atomik olarak yayınlandı.");
      await loadJobs(page);
      await selectJob(selectedJob.id);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Import yayınlanamadı.");
    } finally {
      setBusy(false);
    }
  }

  const totalPages = jobs ? Math.max(1, Math.ceil(jobs.totalCount / pageSize)) : 1;

  return (
    <main className={styles.page}>
      <dialog ref={deleteDialog} className={styles.deleteDialog}
        aria-labelledby="delete-import-title" aria-describedby="delete-import-description"
        onCancel={(event) => { event.preventDefault(); if (!busy) setDeleteTarget(null); }}>
        <h2 id="delete-import-title">Importu sil</h2>
        <p id="delete-import-description">Bu import ve ona ait ürünler kalıcı silinecek.</p>
        <p>{deleteTarget?.originalFileName}</p>
        <div className={styles.actions}>
          <button autoFocus onClick={() => setDeleteTarget(null)} disabled={busy}>İptal</button>
          <button onClick={() => void deleteImport()} disabled={busy}>{busy ? "Siliniyor…" : "Evet, Sil"}</button>
        </div>
      </dialog>
      <header className={styles.header}>
        <div>
          <p className={styles.eyebrow}>EKİPHAN · ADMIN</p>
          <h1>Ürün veri aktarımı</h1>
          <p className={styles.lead}>
            CSV veya XLSX dosyasını önce dry-run ile doğrulayın, sorun raporunu
            inceleyin ve yalnızca temiz job&apos;ları taslak ürünlere yayınlayın.
          </p>
        </div>
        <nav className={styles.headerNav} aria-label="Admin menüsü">
          <a className={styles.backLink} href="/admin/catalog/products">
            Ürün yönetimi
          </a>
          <a className={styles.backLink} href="/admin/catalog/categories">
            Kategoriler
          </a>
          <a className={styles.backLink} href="/admin/catalog/brands">
            Markalar
          </a>
          <a className={styles.backLink} href="/admin/catalog/relations">
            Ürün ilişkileri
          </a>
          <a className={styles.backLink} href="/admin/quotes">
            Teklif yönetimi
          </a>
          <a className={styles.backLink} href="/">
            Siteye dön
          </a>
        </nav>
      </header>

      {error && <div className={styles.error} role="alert">{error}</div>}
      {message && <div className={styles.success} role="status">{message}</div>}
      {productTotal !== null && <p role="status">Toplam ürün: {productTotal}</p>}

      <section className={styles.card} aria-labelledby="upload-title">
        <div>
          <p className={styles.sectionIndex}>01</p>
          <h2 id="upload-title">Dosya yükle</h2>
          <p>En fazla 25 MB. Kabul edilen biçimler: CSV ve XLSX.</p>
        </div>
        <form className={styles.uploadForm} onSubmit={upload}>
          <label htmlFor="import-file">Ürün veri dosyası</label>
          <input
            id="import-file"
            name="file"
            type="file"
            accept=".csv,.xlsx,text/csv,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            required
            disabled={busy}
          />
          <label className={styles.checkbox}>
            <input name="isDryRun" type="checkbox" value="true" defaultChecked />
            <span>Dry-run: ürün tablolarına yazmadan doğrula</span>
          </label>
          <button type="submit" disabled={busy}>
            {busy ? "İşleniyor…" : "Doğrulamayı başlat"}
          </button>
        </form>
      </section>

      <section className={styles.card} aria-labelledby="jobs-title">
        <div>
          <p className={styles.sectionIndex}>02</p>
          <h2 id="jobs-title">Import geçmişi</h2>
          <p>
            {loadingJobs
              ? "Yükleniyor…"
              : jobs
                ? `${jobs.totalCount} job`
                : "Liste görüntülenemedi."}
          </p>
        </div>
        <div className={styles.tableWrap}>
          <table>
            <thead>
              <tr>
                <th>Dosya</th>
                <th>Durum</th>
                <th>Satır</th>
                <th>Hata</th>
                <th>Tarih</th>
                <th><span className={styles.srOnly}>İşlem</span></th>
              </tr>
            </thead>
            <tbody>
              {jobs?.items.map((job) => (
                <tr key={job.id}>
                  <td>{job.originalFileName ?? "API aktarımı"}</td>
                  <td><span className={`${styles.badge} ${styles[`status${job.status}`]}`}>{statusLabels[job.status]}</span></td>
                  <td>{job.totalRowCount}</td>
                  <td>{job.invalidRowCount}</td>
                  <td>{formatDate(job.createdAt)}</td>
                  <td>
                    <button className={styles.textButton} onClick={() => void selectJob(job.id)} disabled={busy}>İncele</button>
                    <button className={styles.textButton} onClick={() => setDeleteTarget(job)} disabled={busy}>Sil</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <nav className={styles.pagination} aria-label="Import sayfaları">
          <button onClick={() => setPage((value) => value - 1)} disabled={page <= 1 || busy}>Önceki</button>
          <span>{page} / {totalPages}</span>
          <button onClick={() => setPage((value) => value + 1)} disabled={page >= totalPages || busy}>Sonraki</button>
        </nav>
      </section>

      {selectedJob && (
        <section className={styles.card} aria-labelledby="detail-title">
          <div>
            <p className={styles.sectionIndex}>03</p>
            <h2 id="detail-title">Job detayı</h2>
            <p className={styles.mono}>{selectedJob.id}</p>
          </div>
          <div className={styles.detail}>
            <dl>
              <div><dt>Durum</dt><dd>{statusLabels[selectedJob.status]}</dd></div>
              <div><dt>Geçerli</dt><dd>{selectedJob.validRowCount}</dd></div>
              <div><dt>Geçersiz</dt><dd>{selectedJob.invalidRowCount}</dd></div>
              <div><dt>Uyarı</dt><dd>{selectedJob.warningCount}</dd></div>
              <div><dt>Tamamlanma</dt><dd>{formatDate(selectedJob.completedAt)}</dd></div>
            </dl>
            <div className={styles.actions}>
              <a href={`/api/admin/imports/${selectedJob.id}/issues.csv`}>
                CSV sorun raporu
              </a>
              {selectedJob.status === 3 && !selectedJob.isDryRun && (
                <button onClick={() => void publish()} disabled={busy}>
                  Taslak ürünlere yayınla
                </button>
              )}
            </div>
            {selectedJob.failureReason && (
              <p className={styles.errorText}>{selectedJob.failureReason}</p>
            )}
          </div>
          <div className={styles.tableWrap}>
            <table>
              <thead><tr><th>Konum</th><th>Seviye</th><th>Kod</th><th>Açıklama</th><th>Ham değer</th></tr></thead>
              <tbody>
                {issues?.items.map((issue) => (
                  <tr key={issue.id}>
                    <td>{issue.sheetName} · {issue.rowNumber}</td>
                    <td>{severityLabels[issue.severity]}</td>
                    <td className={styles.mono}>{issue.code}</td>
                    <td>{issue.message}</td>
                    <td>{issue.rawValue ?? "—"}</td>
                  </tr>
                ))}
                {issues?.items.length === 0 && (
                  <tr><td colSpan={5}>Bu job için kayıtlı sorun yok.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </main>
  );
}
