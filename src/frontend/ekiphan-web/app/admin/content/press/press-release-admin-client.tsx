"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { AdminMediaPicker } from "../../../../components/admin-media-picker";
import type { AdminPressRelease } from "../../../../lib/press-release-admin-types";
import styles from "../gallery/gallery-admin.module.css";

type Draft = {
  id?: string; coverMediaId: string; attachmentMediaId: string;
  publishedAt: string; isPublished: boolean; trTitle: string; trSummary: string;
  trBody: string; enTitle: string; enSummary: string; enBody: string;
  trSeo?: Omit<import("../../../../lib/press-release-admin-types").PressReleaseTranslation, "languageCode" | "title" | "summary" | "body">;
  enSeo?: Omit<import("../../../../lib/press-release-admin-types").PressReleaseTranslation, "languageCode" | "title" | "summary" | "body">;
};

const nowLocal = () => new Date().toISOString().slice(0, 16);
const empty = (): Draft => ({
  coverMediaId: "", attachmentMediaId: "", publishedAt: nowLocal(),
  isPublished: false, trTitle: "", trSummary: "", trBody: "",
  enTitle: "", enSummary: "", enBody: ""
});

export function PressReleaseAdminClient() {
  const [items, setItems] = useState<AdminPressRelease[]>([]);
  const [draft, setDraft] = useState<Draft>(empty);
  const [message, setMessage] = useState<string | null>(null);
  const load = useCallback(async () => {
    const response = await fetch("/api/admin/content/press-releases", { cache: "no-store" });
    if (!response.ok) {
      const body = (await response.json()) as { detail?: string };
      throw new Error(body.detail ?? "Basın yayınları alınamadı.");
    }
    setItems((await response.json()) as AdminPressRelease[]);
  }, []);
  useEffect(() => {
    void load().catch((error: unknown) =>
      setMessage(error instanceof Error ? error.message : "Basın yayınları alınamadı."));
  }, [load]);

  function edit(item: AdminPressRelease) {
    const tr = item.translations.find((text) => text.languageCode === "tr");
    const en = item.translations.find((text) => text.languageCode === "en");
    setDraft({
      id: item.id, coverMediaId: item.coverMediaId ?? "",
      attachmentMediaId: item.attachmentMediaId ?? "",
      publishedAt: item.publishedAt.slice(0, 16), isPublished: item.isPublished,
      trTitle: tr?.title ?? "", trSummary: tr?.summary ?? "", trBody: tr?.body ?? "",
      enTitle: en?.title ?? "", enSummary: en?.summary ?? "", enBody: en?.body ?? "",
      trSeo: tr, enSeo: en
    });
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    const translations = [
      draft.trTitle.trim() ? { ...draft.trSeo, languageCode: "tr", title: draft.trTitle, summary: draft.trSummary, body: draft.trBody || null } : null,
      draft.enTitle.trim() ? { ...draft.enSeo, languageCode: "en", title: draft.enTitle, summary: draft.enSummary, body: draft.enBody || null } : null
    ].filter(Boolean);
    const response = await fetch(
      `/api/admin/content/press-releases${draft.id ? `/${draft.id}` : ""}`,
      { method: draft.id ? "PUT" : "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          coverMediaId: draft.coverMediaId || null,
          attachmentMediaId: draft.attachmentMediaId || null,
          publishedAt: new Date(draft.publishedAt).toISOString(),
          isPublished: draft.isPublished, translations
        }) }
    );
    if (!response.ok) {
      const body = (await response.json()) as { detail?: string };
      setMessage(body.detail ?? "Basın yayını kaydedilemedi.");
      return;
    }
    setDraft(empty()); setMessage("Basın yayını kaydedildi."); await load();
  }

  return (
    <main className={styles.page}>
      <header><div><p>EKİPHAN · İÇERİK</p><h1>Basın odası</h1></div>
        <nav><a href="/admin">Yönetime dön</a><a href="/admin/media">Medya kütüphanesi</a></nav>
      </header>
      {message && <div className={styles.message} role="status">{message}</div>}
      <div className={styles.layout}>
        <section><h2>Yayınlar</h2><div className={styles.list}>
          {items.map((item) => (
            <button type="button" key={item.id} onClick={() => edit(item)}>
              <strong>{item.translations.find((text) => text.languageCode === "tr")?.title ?? item.id}</strong>
              <span>{new Date(item.publishedAt).toLocaleDateString("tr-TR")} · {item.isPublished ? "Yayında" : "Taslak"}</span>
            </button>
          ))}
          {items.length === 0 && <p>Henüz basın yayını yok.</p>}
        </div></section>
        <form onSubmit={save}><h2>{draft.id ? "Yayını düzenle" : "Yeni yayın"}</h2>
          <label>Yayın tarihi<input required type="datetime-local" value={draft.publishedAt} onChange={(e) => setDraft({ ...draft, publishedAt: e.target.value })} /></label>
          <AdminMediaPicker label="Kapak görseli" value={draft.coverMediaId}
            onChange={(coverMediaId) => setDraft({ ...draft, coverMediaId })}
            acceptedTypes={["Image"]} />
          <AdminMediaPicker label="İndirilebilir PDF veya doküman"
            value={draft.attachmentMediaId}
            onChange={(attachmentMediaId) => setDraft({ ...draft, attachmentMediaId })}
            acceptedTypes={["Pdf", "Document"]} />
          <label>Türkçe başlık<input maxLength={250} value={draft.trTitle} onChange={(e) => setDraft({ ...draft, trTitle: e.target.value })} /></label>
          <label>Türkçe özet<textarea required maxLength={1000} value={draft.trSummary} onChange={(e) => setDraft({ ...draft, trSummary: e.target.value })} /></label>
          <label>Türkçe metin<textarea maxLength={20000} value={draft.trBody} onChange={(e) => setDraft({ ...draft, trBody: e.target.value })} /></label>
          <label>İngilizce başlık<input maxLength={250} value={draft.enTitle} onChange={(e) => setDraft({ ...draft, enTitle: e.target.value })} /></label>
          <label>İngilizce özet<textarea maxLength={1000} value={draft.enSummary} onChange={(e) => setDraft({ ...draft, enSummary: e.target.value })} /></label>
          <label>İngilizce metin<textarea maxLength={20000} value={draft.enBody} onChange={(e) => setDraft({ ...draft, enBody: e.target.value })} /></label>
          <label className={styles.check}><input type="checkbox" checked={draft.isPublished} onChange={(e) => setDraft({ ...draft, isPublished: e.target.checked })} />Yayında</label>
          <div className={styles.actions}><button type="submit">Kaydet</button><button type="button" onClick={() => setDraft(empty())}>Yeni kayıt</button></div>
        </form>
      </div>
    </main>
  );
}
