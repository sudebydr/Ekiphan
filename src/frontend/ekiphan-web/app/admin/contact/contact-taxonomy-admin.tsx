"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { useAdminSession } from "../admin-session-guard";
import type {
  AdminComplaintCategory,
  AdminContactReason,
  AdminContactTaxonomy
} from "../../../lib/admin-contact-types";
import styles from "./contact-taxonomy-admin.module.css";

async function errorOf(response: Response): Promise<string> {
  try {
    const value = (await response.json()) as { detail?: string };
    return value.detail ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function ContactTaxonomyAdmin({ onChange }: {
  onChange: (data: AdminContactTaxonomy) => void;
}) {
  const { hasPermission } = useAdminSession();
  const canManage = hasPermission("contacts.manage");
  const [data, setData] = useState<AdminContactTaxonomy>({
    reasons: [], complaintCategories: []
  });
  const [reasonId, setReasonId] = useState<string | null>(null);
  const [reasonName, setReasonName] = useState("");
  const [reasonOrder, setReasonOrder] = useState(0);
  const [reasonActive, setReasonActive] = useState(true);
  const [isComplaintReason, setIsComplaintReason] = useState(false);
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [categoryName, setCategoryName] = useState("");
  const [categoryOrder, setCategoryOrder] = useState(0);
  const [categoryActive, setCategoryActive] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const response = await fetch("/api/admin/contact-requests/taxonomy", {
        cache: "no-store"
      });
      if (!response.ok) throw new Error(await errorOf(response));
      const taxonomy = (await response.json()) as AdminContactTaxonomy;
      setData(taxonomy);
      onChange(taxonomy);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Tanımlar alınamadı.");
    }
  }, [onChange]);

  useEffect(() => { void load(); }, [load]);

  const complaintReason = data.reasons.find((item) => item.isComplaintReason);

  function newReason() {
    setReasonId(null); setReasonName(""); setReasonOrder(data.reasons.length);
    setReasonActive(true); setIsComplaintReason(false);
  }

  function chooseReason(item: AdminContactReason) {
    setReasonId(item.id); setReasonName(item.name); setReasonOrder(item.sortOrder);
    setReasonActive(item.isActive); setIsComplaintReason(item.isComplaintReason);
  }

  function newCategory() {
    setCategoryId(null); setCategoryName("");
    setCategoryOrder(data.complaintCategories.length); setCategoryActive(true);
  }

  function chooseCategory(item: AdminComplaintCategory) {
    setCategoryId(item.id); setCategoryName(item.name);
    setCategoryOrder(item.sortOrder); setCategoryActive(item.isActive);
  }

  async function save(url: string, method: "POST" | "PUT", body: object) {
    setBusy(true); setError(null); setMessage(null);
    try {
      const response = await fetch(url, {
        method, headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
      });
      if (!response.ok) throw new Error(await errorOf(response));
      await load();
      return true;
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Kayıt tamamlanamadı.");
      return false;
    } finally {
      setBusy(false);
    }
  }

  async function saveReason(event: FormEvent) {
    event.preventDefault();
    const ok = await save(
      reasonId ? `/api/admin/contact-requests/reasons/${reasonId}` :
        "/api/admin/contact-requests/reasons",
      reasonId ? "PUT" : "POST",
      { name: reasonName, sortOrder: reasonOrder, isActive: reasonActive,
        isComplaintReason }
    );
    if (ok) setMessage(reasonId ? "İletişim nedeni güncellendi." : "İletişim nedeni eklendi.");
  }

  async function saveCategory(event: FormEvent) {
    event.preventDefault();
    if (!complaintReason) return;
    const ok = await save(
      categoryId ? `/api/admin/contact-requests/complaint-categories/${categoryId}` :
        "/api/admin/contact-requests/complaint-categories",
      categoryId ? "PUT" : "POST",
      { contactReasonId: complaintReason.id, name: categoryName,
        sortOrder: categoryOrder, isActive: categoryActive }
    );
    if (ok) {
      setMessage(categoryId ? "Şikâyet kategorisi güncellendi." : "Şikâyet kategorisi eklendi.");
      newCategory();
    }
  }

  async function deleteReason(item: AdminContactReason) {
    if (!canManage || busy) return;
    if (!window.confirm(`“${item.name}” iletişim nedeni silinsin mi?`)) return;
    setBusy(true); setError(null); setMessage(null);
    try {
      const response = await fetch(`/api/admin/contact-requests/reasons/${item.id}`, {
        method: "DELETE"
      });
      if (response.status === 409) {
        throw new Error("Bu iletişim nedeni mevcut taleplerde kullanıldığı veya kendisine bağlı şikâyet kategorileri bulunduğu için silinemiyor. Bunun yerine nedeni pasif yapabilirsiniz.");
      }
      if (response.status === 403) {
        throw new Error("İletişim nedenlerini silme yetkiniz bulunmuyor.");
      }
      if (!response.ok) throw new Error(await errorOf(response));
      const taxonomy = {
        ...data,
        reasons: data.reasons.filter((reason) => reason.id !== item.id)
      };
      setData(taxonomy);
      onChange(taxonomy);
      if (reasonId === item.id) newReason();
      setMessage("İletişim nedeni silindi.");
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "İletişim nedeni silinemedi.");
    } finally {
      setBusy(false);
    }
  }

  async function deleteCategory() {
    if (!categoryId || !canManage || busy) return;
    if (!window.confirm(`“${categoryName}” kategorisi silinsin mi?`)) return;
    setBusy(true); setError(null); setMessage(null);
    try {
      const response = await fetch(`/api/admin/contact-requests/complaint-categories/${categoryId}`, {
        method: "DELETE"
      });
      if (response.status === 409) {
        throw new Error("Bu kategori mevcut iletişim taleplerinde kullanıldığı için silinemiyor. Bunun yerine kategoriyi pasif yapabilirsiniz.");
      }
      if (!response.ok) throw new Error(await errorOf(response));
      newCategory();
      await load();
      setMessage("Şikâyet kategorisi silindi.");
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Kategori silinemedi.");
    } finally {
      setBusy(false);
    }
  }

  return <section className={styles.section} aria-labelledby="contact-taxonomy-title">
    <header><p>İLETİŞİM TANIMLARI</p><h2 id="contact-taxonomy-title">Nedenler ve şikâyet kategorileri</h2></header>
    {error && <div className={styles.error} role="alert">{error}</div>}
    {message && <div className={styles.success} role="status">{message}</div>}
    <div className={styles.grid}>
      <article className={styles.panel}>
        <div className={styles.panelHeader}><h3>İletişim nedenleri</h3>{canManage && <button type="button" disabled={busy} onClick={newReason}>Yeni neden</button>}</div>
        <div className={styles.list}>{data.reasons.map((item) => <div key={item.id} className={styles.reasonRow}>
          <button type="button" disabled={busy} onClick={() => chooseReason(item)} className={reasonId === item.id ? styles.selected : ""}><strong>{item.name}</strong><span>Sıra {item.sortOrder} · {item.isActive ? "Aktif" : "Pasif"}{item.isComplaintReason ? " · Şikâyet nedeni" : ""}</span></button>
          {canManage && <button type="button" disabled={busy} onClick={() => void deleteReason(item)} aria-label={`${item.name} iletişim nedenini sil`}>Sil</button>}
        </div>)}</div>
        {canManage && <form onSubmit={saveReason} className={styles.form}>
          <label>Ad<input required maxLength={150} value={reasonName} onChange={(e) => setReasonName(e.target.value)} /></label>
          <label>Sıra<input required min={0} type="number" value={reasonOrder} onChange={(e) => setReasonOrder(e.target.valueAsNumber)} /></label>
          <label className={styles.check}><input type="checkbox" checked={reasonActive} onChange={(e) => setReasonActive(e.target.checked)} />Aktif</label>
          <label className={styles.check}><input type="checkbox" checked={isComplaintReason} disabled={Boolean(reasonId)} onChange={(e) => setIsComplaintReason(e.target.checked)} />Müşteri şikâyeti nedeni</label>
          <button disabled={busy || !reasonName.trim()}>Kaydet</button>
        </form>}
      </article>
      <article className={styles.panel}>
        <div className={styles.panelHeader}><h3>Şikâyet kategorileri</h3>{canManage && <button type="button" onClick={newCategory} disabled={busy || !complaintReason}>Yeni kategori</button>}</div>
        {complaintReason && <p className={styles.hint}>Bağlı iletişim nedeni: {complaintReason.name}. Düzenlemek veya silmek için bir kategori seçin.</p>}
        {complaintReason && data.complaintCategories.length === 0 && <p className={styles.hint}>Henüz şikâyet kategorisi eklenmemiş.</p>}
        {!complaintReason && <p className={styles.hint}>Önce “Müşteri şikâyeti nedeni” olarak işaretlenmiş bir iletişim nedeni ekleyin.</p>}
        <div className={styles.list}>{data.complaintCategories.filter((item) => item.contactReasonId === complaintReason?.id).map((item) => <button type="button" key={item.id} disabled={busy} onClick={() => chooseCategory(item)} className={categoryId === item.id ? styles.selected : ""}><strong>{item.name}</strong><span>Sıra {item.sortOrder} · {item.isActive ? "Aktif" : "Pasif"}</span></button>)}</div>
        {canManage && complaintReason && <form onSubmit={saveCategory} className={styles.form}>
          <label>Bağlı neden<input value={complaintReason.name} disabled /></label>
          <label>Ad<input required maxLength={150} value={categoryName} onChange={(e) => setCategoryName(e.target.value)} /></label>
          <label>Sıra<input required min={0} type="number" value={categoryOrder} onChange={(e) => setCategoryOrder(e.target.valueAsNumber)} /></label>
          <label className={styles.check}><input type="checkbox" checked={categoryActive} onChange={(e) => setCategoryActive(e.target.checked)} />Aktif</label>
          <button disabled={busy || !categoryName.trim()}>Kaydet</button>
          {categoryId && <button type="button" disabled={busy} onClick={() => void deleteCategory()}>Kategoriyi sil</button>}
        </form>}
      </article>
    </div>
  </section>;
}
