"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { AdminMediaPicker } from "../../../../components/admin-media-picker";
import type { AdminGalleryItem } from "../../../../lib/gallery-admin-types";
import styles from "./gallery-admin.module.css";

type Draft = {
  id?: string;
  mediaAssetId: string;
  sortOrder: string;
  isPublished: boolean;
  trTitle: string;
  trCaption: string;
  enTitle: string;
  enCaption: string;
};

const emptyDraft: Draft = {
  mediaAssetId: "",
  sortOrder: "0",
  isPublished: false,
  trTitle: "",
  trCaption: "",
  enTitle: "",
  enCaption: ""
};

export function GalleryAdminClient() {
  const [items, setItems] = useState<AdminGalleryItem[]>([]);
  const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    const response = await fetch("/api/admin/content/gallery", { cache: "no-store" });
    if (!response.ok) {
      const body = (await response.json()) as { detail?: string };
      throw new Error(body.detail ?? "Galeri kayıtları alınamadı.");
    }
    setItems((await response.json()) as AdminGalleryItem[]);
  }, []);

  useEffect(() => {
    void load().catch((error: unknown) =>
      setMessage(error instanceof Error ? error.message : "Galeri kayıtları alınamadı.")
    );
  }, [load]);

  function edit(item: AdminGalleryItem) {
    const tr = item.translations.find((text) => text.languageCode === "tr");
    const en = item.translations.find((text) => text.languageCode === "en");
    setDraft({
      id: item.id,
      mediaAssetId: item.mediaAssetId,
      sortOrder: String(item.sortOrder),
      isPublished: item.isPublished,
      trTitle: tr?.title ?? "",
      trCaption: tr?.caption ?? "",
      enTitle: en?.title ?? "",
      enCaption: en?.caption ?? ""
    });
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    setMessage(null);
    const translations = [
      draft.trTitle.trim()
        ? { languageCode: "tr", title: draft.trTitle, caption: draft.trCaption || null }
        : null,
      draft.enTitle.trim()
        ? { languageCode: "en", title: draft.enTitle, caption: draft.enCaption || null }
        : null
    ].filter(Boolean);
    const response = await fetch(
      `/api/admin/content/gallery${draft.id ? `/${draft.id}` : ""}`,
      {
        method: draft.id ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          mediaAssetId: draft.mediaAssetId,
          sortOrder: Number(draft.sortOrder),
          isPublished: draft.isPublished,
          translations
        })
      }
    );
    if (!response.ok) {
      const body = (await response.json()) as { detail?: string };
      setMessage(body.detail ?? "Galeri kaydı kaydedilemedi.");
      return;
    }
    setDraft(emptyDraft);
    setMessage("Galeri kaydı kaydedildi.");
    await load();
  }

  return (
    <main className={styles.page}>
      <header>
        <div><p>EKİPHAN · İÇERİK</p><h1>Galeri yönetimi</h1></div>
        <nav><a href="/admin">Yönetime dön</a><a href="/admin/media">Medya kütüphanesi</a></nav>
      </header>
      {message && <div className={styles.message} role="status">{message}</div>}
      <div className={styles.layout}>
        <section>
          <h2>Galeri kayıtları</h2>
          <div className={styles.list}>
            {items.map((item) => {
              const title = item.translations.find((text) => text.languageCode === "tr")?.title;
              return (
                <button type="button" key={item.id} onClick={() => edit(item)}>
                  <strong>{title ?? item.id}</strong>
                  <span>Sıra {item.sortOrder} · {item.isPublished ? "Yayında" : "Taslak"}</span>
                </button>
              );
            })}
            {items.length === 0 && <p>Henüz galeri kaydı yok.</p>}
          </div>
        </section>
        <form onSubmit={save}>
          <h2>{draft.id ? "Kaydı düzenle" : "Yeni görsel"}</h2>
          <AdminMediaPicker label="Galeri görseli" required
            value={draft.mediaAssetId}
            onChange={(mediaAssetId) => setDraft({ ...draft, mediaAssetId })}
            acceptedTypes={["Image"]} />
          <label>Sıra<input type="number" value={draft.sortOrder} onChange={(e) => setDraft({ ...draft, sortOrder: e.target.value })} /></label>
          <label>Türkçe başlık<input maxLength={200} value={draft.trTitle} onChange={(e) => setDraft({ ...draft, trTitle: e.target.value })} /></label>
          <label>Türkçe açıklama<textarea maxLength={1000} value={draft.trCaption} onChange={(e) => setDraft({ ...draft, trCaption: e.target.value })} /></label>
          <label>İngilizce başlık<input maxLength={200} value={draft.enTitle} onChange={(e) => setDraft({ ...draft, enTitle: e.target.value })} /></label>
          <label>İngilizce açıklama<textarea maxLength={1000} value={draft.enCaption} onChange={(e) => setDraft({ ...draft, enCaption: e.target.value })} /></label>
          <label className={styles.check}><input type="checkbox" checked={draft.isPublished} onChange={(e) => setDraft({ ...draft, isPublished: e.target.checked })} />Yayında</label>
          <div className={styles.actions}><button type="submit">Kaydet</button><button type="button" onClick={() => setDraft(emptyDraft)}>Yeni kayıt</button></div>
        </form>
      </div>
    </main>
  );
}
