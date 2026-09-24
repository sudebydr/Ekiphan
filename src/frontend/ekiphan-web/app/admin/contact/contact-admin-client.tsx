"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAdminSession } from "../admin-session-guard";
import type {
  AdminContactDetail,
  AdminContactPage,
  ContactAssignee,
  ContactStatus
} from "../../../lib/admin-contact-types";
import {
  contactStatusLabels,
  contactStatusOptions,
  contactTransitions
} from "../../../lib/admin-contact-types";
import styles from "../quotes/quotes.module.css";

type Draft = {
  search: string; status: string; dateFrom: string; dateTo: string;
  assignedUserId: string; newOnly: boolean; sort: "Newest" | "Oldest";
  pageSize: string;
};

function draftOf(params: URLSearchParams): Draft {
  return {
    search: params.get("search") ?? "",
    status: params.get("status") ?? "",
    dateFrom: params.get("dateFromInput") ?? "",
    dateTo: params.get("dateToInput") ?? "",
    assignedUserId: params.get("assignedUserId") ?? "",
    newOnly: params.get("newOnly") === "true",
    sort: params.get("sort") === "Oldest" ? "Oldest" : "Newest",
    pageSize: params.get("pageSize") ?? "20"
  };
}

function date(value: string): string {
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium", timeStyle: "short"
  }).format(new Date(value));
}

async function errorOf(response: Response): Promise<string> {
  try {
    const value = (await response.json()) as { detail?: string; title?: string };
    return response.status === 403
      ? "Bu işlem için yetkiniz bulunmuyor."
      : value.detail ?? value.title ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function ContactAdminClient() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const queryKey = searchParams.toString();
  const { hasPermission } = useAdminSession();
  const canManage = hasPermission("contacts.manage");
  const [draft, setDraft] = useState<Draft>(() => draftOf(searchParams));
  const [result, setResult] = useState<AdminContactPage | null>(null);
  const [assignees, setAssignees] = useState<ContactAssignee[]>([]);
  const [selected, setSelected] = useState<AdminContactDetail | null>(null);
  const [targetStatus, setTargetStatus] = useState<ContactStatus | null>(null);
  const [note, setNote] = useState("");
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [unauthorized, setUnauthorized] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const page = Math.max(1, Number(searchParams.get("page") ?? "1") || 1);
  const pageSize = Math.min(100, Math.max(1,
    Number(searchParams.get("pageSize") ?? "20") || 20));

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams(queryKey);
      const from = params.get("dateFromInput");
      const to = params.get("dateToInput");
      params.delete("dateFromInput");
      params.delete("dateToInput");
      if (from) params.set("dateFrom", new Date(`${from}T00:00:00`).toISOString());
      if (to) params.set("dateTo", new Date(`${to}T23:59:59.999`).toISOString());
      const response = await fetch(`/api/admin/contact-requests?${params}`, { cache: "no-store" });
      if (response.status === 403) {
        setUnauthorized(true);
        return;
      }
      if (!response.ok) throw new Error(await errorOf(response));
      setUnauthorized(false);
      setResult((await response.json()) as AdminContactPage);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "İletişim talepleri alınamadı.");
    } finally {
      setLoading(false);
    }
  }, [queryKey]);

  useEffect(() => {
    setDraft(draftOf(searchParams));
    void load();
  }, [load, queryKey, searchParams]);

  useEffect(() => {
    void fetch("/api/admin/contact-requests/assignees", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) throw new Error();
        setAssignees((await response.json()) as ContactAssignee[]);
      }).catch(() => setAssignees([]));
  }, []);

  const detail = useCallback(async (id: string) => {
    setDetailLoading(true);
    setError(null);
    try {
      const response = await fetch(`/api/admin/contact-requests/${id}`, { cache: "no-store" });
      if (!response.ok) throw new Error(await errorOf(response));
      const value = (await response.json()) as AdminContactDetail;
      setSelected(value);
      setTargetStatus(contactTransitions[value.status][0] ?? null);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Talep detayı alınamadı.");
    } finally {
      setDetailLoading(false);
    }
  }, []);

  function field<K extends keyof Draft>(key: K, value: Draft[K]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function filter(event: FormEvent) {
    event.preventDefault();
    const params = new URLSearchParams({ page: "1", pageSize: draft.pageSize, sort: draft.sort });
    if (draft.search.trim()) params.set("search", draft.search.trim());
    if (draft.status) params.set("status", draft.status);
    if (draft.dateFrom) params.set("dateFromInput", draft.dateFrom);
    if (draft.dateTo) params.set("dateToInput", draft.dateTo);
    if (draft.assignedUserId) params.set("assignedUserId", draft.assignedUserId);
    if (draft.newOnly) params.set("newOnly", "true");
    router.replace(`/admin/contact?${params}`);
  }

  function reset() {
    router.replace("/admin/contact?page=1&pageSize=20&sort=Newest");
    setSelected(null);
  }

  function go(next: number) {
    const params = new URLSearchParams(queryKey);
    params.set("page", String(next));
    router.replace(`/admin/contact?${params}`);
  }

  async function mutate(path: string, body: object, success: string) {
    if (!selected) return;
    setBusy(true); setError(null); setMessage(null);
    try {
      const response = await fetch(`/api/admin/contact-requests/${selected.id}/${path}`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
      });
      if (!response.ok) throw new Error(await errorOf(response));
      setMessage(success);
      await Promise.all([detail(selected.id), load()]);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "İşlem tamamlanamadı.");
    } finally {
      setBusy(false);
    }
  }

  async function status(event: FormEvent) {
    event.preventDefault();
    if (!selected || !targetStatus) return;
    if (!window.confirm(`Durum “${contactStatusLabels[targetStatus]}” olarak güncellensin mi?`)) return;
    await mutate("status", { status: targetStatus, expectedVersion: selected.version }, "Talep durumu güncellendi.");
  }

  async function assign(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) return;
    const value = String(new FormData(event.currentTarget).get("assignee") ?? "");
    await mutate("assignment", { assignedToUserId: value || null, expectedVersion: selected.version }, "Atama güncellendi.");
  }

  async function addNote(event: FormEvent) {
    event.preventDefault();
    if (!selected || !note.trim()) return;
    await mutate("notes", { text: note.trim(), expectedVersion: selected.version }, "Dahili not eklendi.");
    setNote("");
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / pageSize)) : 1;
  if (unauthorized) return <section className={styles.stateCard} role="alert"><h1>Yetkisiz erişim</h1><p>İletişim taleplerini görüntüleme yetkiniz bulunmuyor.</p></section>;

  return <div className={styles.page}>
    <header className={styles.header}><div><p className={styles.eyebrow}>OPERASYON · FAZ 7</p><h1>İletişim talepleri</h1><p className={styles.lead}>İletişim kutusunu takip edin, sorumlu atayın ve yanıt sürecini kayda alın.</p></div></header>
    {error && <div className={styles.error} role="alert">{error}</div>}
    {message && <div className={styles.success} role="status">{message}</div>}
    <section className={styles.panel}><div className={styles.sectionHeader}><div><p className={styles.sectionIndex}>01</p><h2>Talep listesi</h2></div><p>{loading ? "Yükleniyor…" : `${result?.totalCount ?? 0} kayıt`}</p></div>
      <form className={styles.filters} onSubmit={filter}>
        <div className={`${styles.field} ${styles.searchField}`}><label htmlFor="contact-search">Arama</label><input id="contact-search" maxLength={100} value={draft.search} onChange={(e) => field("search", e.target.value)} placeholder="Ad, konu, firma" /></div>
        <div className={styles.field}><label htmlFor="contact-status">Durum</label><select id="contact-status" value={draft.status} onChange={(e) => field("status", e.target.value)}><option value="">Tümü</option>{contactStatusOptions.map(([value, item]) => <option value={value} key={value}>{contactStatusLabels[item]}</option>)}</select></div>
        <div className={styles.field}><label htmlFor="contact-assignee">Atanan</label><select id="contact-assignee" value={draft.assignedUserId} onChange={(e) => field("assignedUserId", e.target.value)}><option value="">Tümü</option><option value="unassigned">Atanmamış</option>{assignees.map((item) => <option key={item.id} value={item.id}>{item.displayName}</option>)}</select></div>
        <div className={styles.field}><label htmlFor="contact-from">Başlangıç</label><input id="contact-from" type="date" value={draft.dateFrom} onChange={(e) => field("dateFrom", e.target.value)} /></div>
        <div className={styles.field}><label htmlFor="contact-to">Bitiş</label><input id="contact-to" type="date" value={draft.dateTo} onChange={(e) => field("dateTo", e.target.value)} /></div>
        <div className={styles.field}><label htmlFor="contact-sort">Sıralama</label><select id="contact-sort" value={draft.sort} onChange={(e) => field("sort", e.target.value as Draft["sort"])}><option value="Newest">En yeni</option><option value="Oldest">En eski</option></select></div>
        <div className={styles.field}><label htmlFor="contact-size">Sayfa boyutu</label><select id="contact-size" value={draft.pageSize} onChange={(e) => field("pageSize", e.target.value)}>{[10, 20, 50, 100].map((size) => <option key={size}>{size}</option>)}</select></div>
        <label className={styles.checkbox}><input type="checkbox" checked={draft.newOnly} onChange={(e) => field("newOnly", e.target.checked)} /> Yalnızca yeni</label>
        <div className={styles.actions}><button>Filtrele</button><button type="button" className={styles.secondaryButton} onClick={reset}>Temizle</button></div>
      </form>
      <div className={styles.tableWrap}><table><thead><tr><th>Tarih</th><th>Gönderen</th><th>İletişim</th><th>Konu / Özet</th><th>Durum</th><th>Atanan</th><th>Son işlem</th><th><span className={styles.srOnly}>İşlem</span></th></tr></thead><tbody>
        {loading && Array.from({ length: 5 }, (_, row) => <tr key={row} className={styles.skeletonRow}><td colSpan={8}><span /></td></tr>)}
        {!loading && result?.items.map((item) => <tr key={item.id}><td>{date(item.createdAt)}</td><td>{item.fullName}</td><td>{item.maskedEmail}<br /><small>{item.maskedPhone ?? "—"}</small></td><td><strong>{item.subject}</strong><br /><small>{item.messagePreview}</small></td><td><span className={`${styles.badge} ${styles[`status${item.status}`]}`}>{contactStatusLabels[item.status]}</span></td><td>{item.assignedToDisplayName ?? "Atanmamış"}</td><td>{date(item.updatedAt)}</td><td><button type="button" className={styles.textButton} onClick={() => void detail(item.id)}>İncele</button></td></tr>)}
        {!loading && result?.items.length === 0 && <tr><td colSpan={8} className={styles.empty}>Filtrelerle eşleşen iletişim talebi yok.</td></tr>}
      </tbody></table></div>
      <nav className={styles.pagination}><button type="button" disabled={page <= 1} onClick={() => go(page - 1)}>Önceki</button><span>{page} / {totalPages}</span><button type="button" disabled={page >= totalPages} onClick={() => go(page + 1)}>Sonraki</button></nav>
    </section>
    {detailLoading && <section className={styles.panel} aria-busy="true"><p role="status">Talep detayı yükleniyor…</p></section>}
    {selected && !detailLoading && <section className={styles.panel}><div className={styles.sectionHeader}><div><p className={styles.sectionIndex}>02</p><h2>{selected.subject}</h2></div><span className={`${styles.badge} ${styles[`status${selected.status}`]}`}>{contactStatusLabels[selected.status]}</span></div>
      <div className={styles.detailGrid}><div className={styles.contact}><dl><div><dt>Ad soyad</dt><dd>{selected.fullName}</dd></div><div><dt>Firma</dt><dd>{selected.companyName ?? "—"}</dd></div><div><dt>E-posta</dt><dd><a href={`mailto:${selected.email}`}>{selected.email}</a></dd></div><div><dt>Telefon</dt><dd>{selected.phone ? <a href={`tel:${selected.phone}`}>{selected.phone}</a> : "—"}</dd></div><div><dt>Atanan</dt><dd>{selected.assignedToDisplayName ?? "Atanmamış"}</dd></div><div><dt>Oluşturulma</dt><dd>{date(selected.createdAt)}</dd></div><div><dt>Son işlem</dt><dd>{date(selected.updatedAt)}</dd></div><div><dt>KVKK kaydı</dt><dd>{date(selected.consentAt)} · {selected.consentVersion}</dd></div></dl><div className={styles.message}>{selected.message}</div></div>
        <div>{canManage ? <div className={styles.managementGrid}><form className={styles.compactForm} onSubmit={assign}><label htmlFor="contact-detail-assignee">Sorumlu kullanıcı</label><select id="contact-detail-assignee" name="assignee" defaultValue={selected.assignedToUserId ?? ""}><option value="">Atanmamış</option>{assignees.map((item) => <option key={item.id} value={item.id}>{item.displayName}</option>)}</select><button disabled={busy}>Atamayı kaydet</button></form>
          {contactTransitions[selected.status].length > 0 && <form className={styles.compactForm} onSubmit={status}><label htmlFor="contact-target-status">Yeni durum</label><select id="contact-target-status" value={targetStatus ?? ""} onChange={(e) => setTargetStatus(Number(e.target.value) as ContactStatus)}>{contactTransitions[selected.status].map((item) => <option key={item} value={item}>{contactStatusLabels[item]}</option>)}</select><button disabled={busy}>Durumu güncelle</button></form>}
          <form className={styles.compactForm} onSubmit={addNote}><label htmlFor="contact-note">Dahili not</label><textarea id="contact-note" required maxLength={2000} value={note} onChange={(e) => setNote(e.target.value)} /><button disabled={busy || !note.trim()}>Not ekle</button></form></div> : <p className={styles.readOnly}>Bu kayıt salt okunur görüntüleniyor.</p>}</div></div>
      <div className={styles.auditGrid}><section><h3>Durum geçmişi</h3><ol className={styles.history}>{selected.statusHistory.map((item) => <li key={item.id}><time>{date(item.changedAt)}</time><div><strong>{item.fromStatus ? `${contactStatusLabels[item.fromStatus]} → ` : ""}{contactStatusLabels[item.toStatus]}</strong><br /><small>{item.changedByDisplayName ?? "Sistem"}</small></div></li>)}</ol></section><section><h3>Dahili notlar</h3><ol className={styles.history}>{selected.internalNotes.map((item) => <li key={item.id}><time>{date(item.recordedAt)}</time><div><p>{item.text}</p><small>{item.authorDisplayName}</small></div></li>)}{selected.internalNotes.length === 0 && <li className={styles.empty}>Henüz dahili not yok.</li>}</ol></section></div>
    </section>}
  </div>;
}
