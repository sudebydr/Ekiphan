"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { useAdminSession } from "../../admin-session-guard";
import type { AdminComplaintPage, AdminContactDetail, ContactAssignee, ContactStatus } from "../../../../lib/admin-contact-types";
import { contactStatusLabels, contactStatusOptions, contactTransitions } from "../../../../lib/admin-contact-types";
import styles from "../../quotes/quotes.module.css";

const formatDate = (value: string) => new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
async function errorOf(response: Response) { try { const body = await response.json() as { detail?: string }; return body.detail ?? "İşlem tamamlanamadı."; } catch { return "İşlem tamamlanamadı."; } }

export function ComplaintAdminClient() {
  const { hasPermission } = useAdminSession();
  const canManage = hasPermission("contacts.manage");
  const [result, setResult] = useState<AdminComplaintPage | null>(null);
  const [selected, setSelected] = useState<AdminContactDetail | null>(null);
  const [assignees, setAssignees] = useState<ContactAssignee[]>([]);
  const [search, setSearch] = useState(""); const [statusFilter, setStatusFilter] = useState("");
  const [page, setPage] = useState(1); const [targetStatus, setTargetStatus] = useState<ContactStatus | null>(null);
  const [note, setNote] = useState(""); const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null); const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    const params = new URLSearchParams({ page: String(page), pageSize: "20" });
    if (search.trim()) params.set("search", search.trim()); if (statusFilter) params.set("status", statusFilter);
    try { const response = await fetch(`/api/admin/contact-requests/complaints?${params}`, { cache: "no-store" }); if (!response.ok) throw new Error(await errorOf(response)); setResult(await response.json() as AdminComplaintPage); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Şikâyetler alınamadı."); }
  }, [page, search, statusFilter]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => { void fetch("/api/admin/contact-requests/assignees", { cache: "no-store" }).then(async r => { if (r.ok) setAssignees(await r.json() as ContactAssignee[]); }); }, []);

  async function detail(id: string) { setError(null); const response = await fetch(`/api/admin/contact-requests/${id}`, { cache: "no-store" }); if (!response.ok) { setError(await errorOf(response)); return; } const value = await response.json() as AdminContactDetail; setSelected(value); setTargetStatus(contactTransitions[value.status][0] ?? null); }
  async function mutate(path: string, body: object, success: string) { if (!selected) return; setBusy(true); setError(null); try { const response = await fetch(`/api/admin/contact-requests/${selected.id}/${path}`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) }); if (!response.ok) throw new Error(await errorOf(response)); setMessage(success); await Promise.all([detail(selected.id), load()]); } catch (reason) { setError(reason instanceof Error ? reason.message : "İşlem tamamlanamadı."); } finally { setBusy(false); } }

  const totalPages = Math.max(1, Math.ceil((result?.totalCount ?? 0) / 20));
  return <div className={styles.page}>
    <header className={styles.header}><div><p className={styles.eyebrow}>OPERASYON · İLETİŞİM</p><h1>Müşteri Şikâyetleri</h1><p className={styles.lead}>İletişim formundan gelen müşteri şikâyetlerini takip edin.</p></div></header>
    {error && <div className={styles.error} role="alert">{error}</div>}{message && <div className={styles.success} role="status">{message}</div>}
    <section className={styles.panel}><form className={styles.filters} onSubmit={(e) => { e.preventDefault(); setPage(1); void load(); }}><div className={`${styles.field} ${styles.searchField}`}><label>Arama</label><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Ad, e-posta, konu, firma" /></div><div className={styles.field}><label>Durum</label><select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}><option value="">Tümü</option>{contactStatusOptions.map(([value, item]) => <option key={value} value={value}>{contactStatusLabels[item]}</option>)}</select></div><div className={styles.actions}><button>Filtrele</button></div></form>
      <div className={styles.tableWrap}><table><thead><tr><th>Tarih</th><th>Ad soyad</th><th>E-posta</th><th>Telefon</th><th>Firma</th><th>Kategori</th><th>Konu</th><th>Durum</th><th /></tr></thead><tbody>{result?.items.map(item => <tr key={item.id}><td>{formatDate(item.createdAt)}</td><td>{item.fullName}</td><td>{item.email}</td><td>{item.phone ?? "—"}</td><td>{item.companyName ?? "—"}</td><td>{item.categoryName}</td><td>{item.subject}</td><td><span className={`${styles.badge} ${styles[`status${item.status}`]}`}>{contactStatusLabels[item.status]}</span></td><td><button type="button" className={styles.textButton} onClick={() => void detail(item.id)}>İncele</button></td></tr>)}</tbody></table></div>
      <nav className={styles.pagination}><button disabled={page <= 1} onClick={() => setPage(page - 1)}>Önceki</button><span>{page} / {totalPages}</span><button disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Sonraki</button></nav>
    </section>
    {selected && <section className={styles.panel}><div className={styles.sectionHeader}><div><p className={styles.sectionIndex}>DETAY</p><h2>{selected.subject}</h2></div><span className={`${styles.badge} ${styles[`status${selected.status}`]}`}>{contactStatusLabels[selected.status]}</span></div>
      <div className={styles.detailGrid}><div className={styles.contact}><dl><div><dt>Ad soyad</dt><dd>{selected.fullName}</dd></div><div><dt>E-posta</dt><dd><a href={`mailto:${selected.email}`}>{selected.email}</a></dd></div><div><dt>Telefon</dt><dd>{selected.phone ?? "—"}</dd></div><div><dt>Firma</dt><dd>{selected.companyName ?? "—"}</dd></div><div><dt>Kategori</dt><dd>{selected.complaintCategoryName ?? "—"}</dd></div><div><dt>Tarih</dt><dd>{formatDate(selected.createdAt)}</dd></div></dl><div className={styles.message}>{selected.message}</div></div>
        {canManage && <div className={styles.managementGrid}><form className={styles.compactForm} onSubmit={async e => { e.preventDefault(); const value = String(new FormData(e.currentTarget).get("assignee") ?? ""); await mutate("assignment", { assignedToUserId: value || null, expectedVersion: selected.version }, "Atama güncellendi."); }}><label>Sorumlu</label><select name="assignee" defaultValue={selected.assignedToUserId ?? ""}><option value="">Atanmamış</option>{assignees.map(item => <option key={item.id} value={item.id}>{item.displayName}</option>)}</select><button disabled={busy}>Kaydet</button></form>
          {contactTransitions[selected.status].length > 0 && <form className={styles.compactForm} onSubmit={async e => { e.preventDefault(); if (targetStatus) await mutate("status", { status: targetStatus, expectedVersion: selected.version }, "Durum güncellendi."); }}><label>Yeni durum</label><select value={targetStatus ?? ""} onChange={e => setTargetStatus(Number(e.target.value) as ContactStatus)}>{contactTransitions[selected.status].map(item => <option key={item} value={item}>{contactStatusLabels[item]}</option>)}</select><button disabled={busy}>Güncelle</button></form>}
          <form className={styles.compactForm} onSubmit={async (e: FormEvent) => { e.preventDefault(); if (note.trim()) { await mutate("notes", { text: note.trim(), expectedVersion: selected.version }, "Not eklendi."); setNote(""); } }}><label>Dahili not</label><textarea required maxLength={2000} value={note} onChange={e => setNote(e.target.value)} /><button disabled={busy || !note.trim()}>Not ekle</button></form></div>}
      </div><div className={styles.auditGrid}><section><h3>Durum geçmişi</h3><ol className={styles.history}>{selected.statusHistory.map(item => <li key={item.id}><time>{formatDate(item.changedAt)}</time><strong>{item.fromStatus ? `${contactStatusLabels[item.fromStatus]} → ` : ""}{contactStatusLabels[item.toStatus]}</strong></li>)}</ol></section><section><h3>Dahili notlar</h3><ol className={styles.history}>{selected.internalNotes.map(item => <li key={item.id}><time>{formatDate(item.recordedAt)}</time><p>{item.text}</p><small>{item.authorDisplayName}</small></li>)}</ol></section></div>
    </section>}
  </div>;
}
