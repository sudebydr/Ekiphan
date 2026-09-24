"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAdminSession } from "../admin-session-guard";
import type {
  AdminAssignee,
  AdminQuoteDetail,
  AdminQuotePagedResult,
  ProblemDetails,
  QuoteStatus
} from "../../../lib/admin-quote-types";
import {
  quoteStatusFilterValues,
  quoteStatusLabels,
  quoteStatusTransitions
} from "../../../lib/admin-quote-types";
import styles from "./quotes.module.css";

type FilterDraft = {
  search: string;
  status: string;
  dateFrom: string;
  dateTo: string;
  assignedUserId: string;
  product: string;
  newOnly: boolean;
  sort: "Newest" | "Oldest";
  pageSize: string;
};

const emptyFilters: FilterDraft = {
  search: "", status: "", dateFrom: "", dateTo: "",
  assignedUserId: "", product: "", newOnly: false,
  sort: "Newest", pageSize: "20"
};

async function readError(response: Response): Promise<string> {
  try {
    const value = (await response.json()) as ProblemDetails;
    if (response.status === 403) return "Bu işlem için yetkiniz bulunmuyor.";
    return value.detail ?? value.title ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

function formatDate(value: string | null): string {
  return value ? new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium", timeStyle: "short"
  }).format(new Date(value)) : "—";
}

function fromQuery(params: URLSearchParams): FilterDraft {
  return {
    search: params.get("search") ?? "",
    status: params.get("status") ?? "",
    dateFrom: params.get("dateFromInput") ?? "",
    dateTo: params.get("dateToInput") ?? "",
    assignedUserId: params.get("assignedUserId") ?? "",
    product: params.get("product") ?? "",
    newOnly: params.get("newOnly") === "true",
    sort: params.get("sort") === "Oldest" ? "Oldest" : "Newest",
    pageSize: params.get("pageSize") ?? "20"
  };
}

export function QuoteAdminClient() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const queryKey = searchParams.toString();
  const { hasPermission } = useAdminSession();
  const canManage = hasPermission("quotes.manage");
  const [draft, setDraft] = useState<FilterDraft>(() => fromQuery(searchParams));
  const [result, setResult] = useState<AdminQuotePagedResult | null>(null);
  const [assignees, setAssignees] = useState<AdminAssignee[]>([]);
  const [selected, setSelected] = useState<AdminQuoteDetail | null>(null);
  const [targetStatus, setTargetStatus] = useState<QuoteStatus | null>(null);
  const [statusNote, setStatusNote] = useState("");
  const [noteText, setNoteText] = useState("");
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [unauthorized, setUnauthorized] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const page = Math.max(1, Number(searchParams.get("page") ?? "1") || 1);
  const pageSize = Math.min(100, Math.max(1,
    Number(searchParams.get("pageSize") ?? "20") || 20));

  const loadList = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams(queryKey);
      const from = params.get("dateFromInput");
      const to = params.get("dateToInput");
      params.delete("dateFromInput");
      params.delete("dateToInput");
      if (from) {
        params.set("dateFrom", new Date(`${from}T00:00:00`).toISOString());
      }
      if (to) {
        params.set("dateTo", new Date(`${to}T23:59:59.999`).toISOString());
      }
      const response = await fetch(`/api/admin/quotes?${params}`, { cache: "no-store" });
      if (response.status === 403) {
        setUnauthorized(true);
        return;
      }
      if (!response.ok) throw new Error(await readError(response));
      setUnauthorized(false);
      setResult((await response.json()) as AdminQuotePagedResult);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Teklif listesi alınamadı.");
    } finally {
      setLoading(false);
    }
  }, [queryKey]);

  useEffect(() => {
    setDraft(fromQuery(searchParams));
    void loadList();
  }, [loadList, queryKey, searchParams]);

  useEffect(() => {
    void fetch("/api/admin/quotes/assignees", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) throw new Error(await readError(response));
        setAssignees((await response.json()) as AdminAssignee[]);
      })
      .catch(() => setAssignees([]));
  }, []);

  const loadDetail = useCallback(async (id: string) => {
    setDetailLoading(true);
    setError(null);
    try {
      const response = await fetch(`/api/admin/quotes/${id}`, { cache: "no-store" });
      if (!response.ok) throw new Error(await readError(response));
      const detail = (await response.json()) as AdminQuoteDetail;
      setSelected(detail);
      setTargetStatus(quoteStatusTransitions[detail.status][0] ?? null);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Teklif detayı alınamadı.");
    } finally {
      setDetailLoading(false);
    }
  }, []);

  useEffect(() => {
    const quoteId = searchParams.get("quoteId");
    if (quoteId) void loadDetail(quoteId);
  }, [loadDetail, searchParams]);
  function setField<K extends keyof FilterDraft>(key: K, value: FilterDraft[K]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function applyFilters(event: FormEvent) {
    event.preventDefault();
    const params = new URLSearchParams();
    params.set("page", "1");
    params.set("pageSize", draft.pageSize);
    if (draft.search.trim()) params.set("search", draft.search.trim());
    if (draft.status) params.set("status", draft.status);
    if (draft.dateFrom) params.set("dateFromInput", draft.dateFrom);
    if (draft.dateTo) params.set("dateToInput", draft.dateTo);
    if (draft.assignedUserId) params.set("assignedUserId", draft.assignedUserId);
    if (draft.product.trim()) params.set("product", draft.product.trim());
    if (draft.newOnly) params.set("newOnly", "true");
    params.set("sort", draft.sort);
    router.replace(`/admin/quotes?${params}`);
  }

  function clearFilters() {
    setDraft(emptyFilters);
    router.replace("/admin/quotes?page=1&pageSize=20&sort=Newest");
    setSelected(null);
  }

  function goToPage(next: number) {
    const params = new URLSearchParams(queryKey);
    params.set("page", String(next));
    router.replace(`/admin/quotes?${params}`);
  }

  async function mutate(path: string, payload: object, success: string) {
    if (!selected) return;
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const response = await fetch(`/api/admin/quotes/${selected.id}/${path}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });
      if (!response.ok) throw new Error(await readError(response));
      setMessage(success);
      await Promise.all([loadDetail(selected.id), loadList()]);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "İşlem tamamlanamadı.");
    } finally {
      setBusy(false);
    }
  }

  async function changeStatus(event: FormEvent) {
    event.preventDefault();
    if (!selected || !targetStatus) return;
    if (!window.confirm(`Durum “${quoteStatusLabels[targetStatus]}” olarak güncellensin mi?`)) return;
    await mutate("status", {
      status: targetStatus,
      expectedVersion: selected.version,
      note: statusNote.trim() || null
    }, "Teklif durumu güncellendi.");
    setStatusNote("");
  }

  async function assign(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) return;
    const value = String(new FormData(event.currentTarget).get("assignee") ?? "");
    await mutate("assignment", {
      assignedToUserId: value || null,
      expectedVersion: selected.version
    }, value ? "Teklif kullanıcıya atandı." : "Teklif ataması kaldırıldı.");
  }

  async function addNote(event: FormEvent) {
    event.preventDefault();
    if (!selected || !noteText.trim()) return;
    await mutate("notes", {
      text: noteText.trim(), expectedVersion: selected.version
    }, "Dahili not eklendi.");
    setNoteText("");
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / pageSize)) : 1;
  const transitions = selected ? quoteStatusTransitions[selected.status] : [];
  const skeletonRows = useMemo(() => Array.from({ length: 5 }, (_, i) => i), []);

  if (unauthorized) {
    return <section className={styles.stateCard} role="alert">
      <h1>Yetkisiz erişim</h1><p>Teklif taleplerini görüntüleme yetkiniz bulunmuyor.</p>
    </section>;
  }

  return <div className={styles.page}>
    <header className={styles.header}>
      <div><p className={styles.eyebrow}>OPERASYON · FAZ 7</p>
        <h1>Teklif talepleri</h1>
        <p className={styles.lead}>Talepleri filtreleyin, sorumlu atayın ve işlem geçmişini güvenle yönetin.</p>
      </div>
    </header>

    {error && <div className={styles.error} role="alert">{error}</div>}
    {message && <div className={styles.success} role="status">{message}</div>}

    <section className={styles.panel} aria-labelledby="quote-list-title">
      <div className={styles.sectionHeader}><div><p className={styles.sectionIndex}>01</p>
        <h2 id="quote-list-title">Talep listesi</h2></div>
        <p>{loading ? "Yükleniyor…" : `${result?.totalCount ?? 0} kayıt`}</p></div>
      <form className={styles.filters} onSubmit={applyFilters}>
        <div className={`${styles.field} ${styles.searchField}`}><label htmlFor="quote-search">Arama</label>
          <input id="quote-search" type="search" maxLength={100} value={draft.search}
            onChange={(e) => setField("search", e.target.value)} placeholder="Talep no, kişi veya firma" /></div>
        <div className={styles.field}><label htmlFor="quote-product">Ürün / SKU</label>
          <input id="quote-product" maxLength={100} value={draft.product}
            onChange={(e) => setField("product", e.target.value)} /></div>
        <div className={styles.field}><label htmlFor="quote-status">Durum</label>
          <select id="quote-status" value={draft.status} onChange={(e) => setField("status", e.target.value)}>
            <option value="">Tümü</option>{quoteStatusFilterValues.map(([value, status]) =>
              <option value={value} key={value}>{quoteStatusLabels[status]}</option>)}</select></div>
        <div className={styles.field}><label htmlFor="quote-assignee">Atanan</label>
          <select id="quote-assignee" value={draft.assignedUserId} onChange={(e) => setField("assignedUserId", e.target.value)}>
            <option value="">Tümü</option><option value="unassigned">Atanmamış</option>
            {assignees.map((item) => <option value={item.id} key={item.id}>{item.displayName}</option>)}</select></div>
        <div className={styles.field}><label htmlFor="quote-from">Başlangıç</label>
          <input id="quote-from" type="date" value={draft.dateFrom} onChange={(e) => setField("dateFrom", e.target.value)} /></div>
        <div className={styles.field}><label htmlFor="quote-to">Bitiş</label>
          <input id="quote-to" type="date" value={draft.dateTo} onChange={(e) => setField("dateTo", e.target.value)} /></div>
        <div className={styles.field}><label htmlFor="quote-sort">Sıralama</label>
          <select id="quote-sort" value={draft.sort} onChange={(e) => setField("sort", e.target.value as FilterDraft["sort"])}>
            <option value="Newest">En yeni</option><option value="Oldest">En eski</option></select></div>
        <div className={styles.field}><label htmlFor="quote-page-size">Sayfa boyutu</label>
          <select id="quote-page-size" value={draft.pageSize} onChange={(e) => setField("pageSize", e.target.value)}>
            {[10, 20, 50, 100].map((size) => <option key={size}>{size}</option>)}</select></div>
        <label className={styles.checkbox}><input type="checkbox" checked={draft.newOnly}
          onChange={(e) => setField("newOnly", e.target.checked)} /> Yalnızca yeni talepler</label>
        <div className={styles.actions}><button type="submit">Filtrele</button>
          <button type="button" className={styles.secondaryButton} onClick={clearFilters}>Temizle</button></div>
      </form>

      <div className={styles.tableWrap}><table><thead><tr>
        <th>Talep / Tarih</th><th>Müşteri</th><th>İletişim</th><th>Ülke</th>
        <th>Ürün / SKU</th><th>Durum</th><th>Atanan</th><th>Son işlem</th><th><span className={styles.srOnly}>İşlem</span></th>
      </tr></thead><tbody>
        {loading && skeletonRows.map((row) => <tr key={row} className={styles.skeletonRow}><td colSpan={9}><span /></td></tr>)}
        {!loading && result?.items.map((quote) => <tr key={quote.id}>
          <td><strong className={styles.mono}>{quote.requestNumber}</strong><br /><small>{formatDate(quote.createdAt)}</small></td>
          <td>{quote.fullName}<br /><small>{quote.companyName}</small></td>
          <td>{quote.maskedEmail}<br /><small>{quote.maskedPhone}</small></td>
          <td>{quote.country}</td>
          <td>{quote.productName ?? "—"}<br /><small className={styles.mono}>{quote.sku ?? "—"}</small></td>
          <td><span className={`${styles.badge} ${styles[`status${quote.status}`]}`}>{quoteStatusLabels[quote.status]}</span></td>
          <td>{quote.assignedToDisplayName ?? "Atanmamış"}</td><td>{formatDate(quote.updatedAt)}</td>
          <td><button type="button" className={styles.textButton} onClick={() => void loadDetail(quote.id)}>İncele</button></td>
        </tr>)}
        {!loading && result?.items.length === 0 && <tr><td colSpan={9} className={styles.empty}>Filtrelerle eşleşen teklif talebi yok.</td></tr>}
      </tbody></table></div>
      <nav className={styles.pagination} aria-label="Teklif sayfaları"><button type="button" disabled={page <= 1} onClick={() => goToPage(page - 1)}>Önceki</button>
        <span>{page} / {totalPages}</span><button type="button" disabled={page >= totalPages} onClick={() => goToPage(page + 1)}>Sonraki</button></nav>
    </section>

    {detailLoading && <section className={styles.panel} aria-busy="true"><p role="status">Talep detayı yükleniyor…</p></section>}
    {selected && !detailLoading && <section className={styles.panel} aria-labelledby="quote-detail-title">
      <div className={styles.sectionHeader}><div><p className={styles.sectionIndex}>02</p><h2 id="quote-detail-title">{selected.requestNumber}</h2></div>
        <span className={`${styles.badge} ${styles[`status${selected.status}`]}`}>{quoteStatusLabels[selected.status]}</span></div>
      <div className={styles.detailGrid}><div className={styles.contact}><dl>
        <div><dt>Ad soyad</dt><dd>{selected.fullName}</dd></div><div><dt>Firma</dt><dd>{selected.companyName}</dd></div>
        <div><dt>E-posta</dt><dd><a href={`mailto:${selected.email}`}>{selected.email}</a></dd></div>
        <div><dt>Telefon</dt><dd><a href={`tel:${selected.phone}`}>{selected.phone}</a></dd></div>
        <div><dt>Konum</dt><dd>{selected.country}{selected.city ? ` · ${selected.city}` : ""}</dd></div>
        <div><dt>Atanan</dt><dd>{selected.assignedToDisplayName ?? "Atanmamış"}</dd></div>
        <div><dt>Oluşturulma</dt><dd>{formatDate(selected.createdAt)}</dd></div><div><dt>Son işlem</dt><dd>{formatDate(selected.updatedAt)}</dd></div>
      </dl>{selected.message && <div className={styles.message}>{selected.message}</div>}</div>
      <div><div className={styles.tableWrap}><table><thead><tr><th>Ürün</th><th>SKU / Varyant</th><th>Adet</th><th>Not</th></tr></thead><tbody>
        {selected.items.map((item) => <tr key={item.id}><td>{item.productId ? <Link href={`/admin/catalog/products?search=${encodeURIComponent(item.sku)}`}>{item.productName}</Link> : item.productName}</td>
          <td><span className={styles.mono}>{item.sku}</span><br /><small>{item.variantSnapshot ?? "—"}</small></td><td>{item.quantity}</td><td>{item.productNote ?? "—"}</td></tr>)}</tbody></table></div>
        {canManage && <div className={styles.managementGrid}>
          <form onSubmit={assign} className={styles.compactForm}><label htmlFor="detail-assignee">Sorumlu kullanıcı</label>
            <select id="detail-assignee" name="assignee" defaultValue={selected.assignedToUserId ?? ""}><option value="">Atanmamış</option>{assignees.map((item) => <option key={item.id} value={item.id}>{item.displayName}</option>)}</select><button disabled={busy}>Atamayı kaydet</button></form>
          {transitions.length > 0 && <form onSubmit={changeStatus} className={styles.compactForm}><label htmlFor="target-status">Yeni durum</label>
            <select id="target-status" value={targetStatus ?? ""} onChange={(e) => setTargetStatus(Number(e.target.value) as QuoteStatus)}>{transitions.map((status) => <option key={status} value={status}>{quoteStatusLabels[status]}</option>)}</select>
            <label htmlFor="status-note">Durum notu</label><textarea id="status-note" maxLength={1000} value={statusNote} onChange={(e) => setStatusNote(e.target.value)} /><button disabled={busy}>Durumu güncelle</button></form>}
          <form onSubmit={addNote} className={styles.compactForm}><label htmlFor="quote-note">Dahili not</label><textarea id="quote-note" required maxLength={2000} value={noteText} onChange={(e) => setNoteText(e.target.value)} /><button disabled={busy || !noteText.trim()}>Not ekle</button></form>
        </div>}
        {!canManage && <p className={styles.readOnly}>Bu kayıt salt okunur görüntüleniyor.</p>}
      </div></div>
      <div className={styles.auditGrid}><section><h3>Durum geçmişi</h3><ol className={styles.history}>{selected.statusHistory.map((item) => <li key={item.id}><time>{formatDate(item.changedAt)}</time><div><strong>{item.fromStatus ? `${quoteStatusLabels[item.fromStatus]} → ` : ""}{quoteStatusLabels[item.toStatus]}</strong><p>{item.note ?? "Not eklenmedi."}</p><small>{item.changedByDisplayName ?? "Sistem"}</small></div></li>)}</ol></section>
        <section><h3>Dahili notlar</h3><ol className={styles.history}>{selected.internalNotes.map((item) => <li key={item.id}><time>{formatDate(item.recordedAt)}</time><div><p>{item.text}</p><small>{item.authorDisplayName}</small></div></li>)}{selected.internalNotes.length === 0 && <li className={styles.empty}>Henüz dahili not yok.</li>}</ol></section></div>
    </section>}
  </div>;
}
