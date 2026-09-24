"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import type { AdminMenuItem } from "../../../../lib/menu-types";
import styles from "./menu.module.css";

async function errorOf(response: Response) {
  try {
    const value = (await response.json()) as { detail?: string };
    return value.detail ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function MenuAdminClient() {
  const [items, setItems] = useState<AdminMenuItem[]>([]);
  const [id, setId] = useState<string | null>(null);
  const [code, setCode] = useState("");
  const [location, setLocation] = useState("Header");
  const [url, setUrl] = useState("/");
  const [external, setExternal] = useState(false);
  const [newTab, setNewTab] = useState(false);
  const [parentId, setParentId] = useState("");
  const [sortOrder, setSortOrder] = useState(0);
  const [published, setPublished] = useState(false);
  const [trLabel, setTrLabel] = useState("");
  const [enLabel, setEnLabel] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    const response = await fetch("/api/admin/content/menu-items", {
      cache: "no-store"
    });
    if (!response.ok) throw new Error(await errorOf(response));
    setItems((await response.json()) as AdminMenuItem[]);
  }, []);

  useEffect(() => {
    void load().catch((reason: unknown) =>
      setError(reason instanceof Error ? reason.message : "Menü alınamadı.")
    );
  }, [load]);

  function reset() {
    setId(null);
    setCode("");
    setLocation("Header");
    setUrl("/");
    setExternal(false);
    setNewTab(false);
    setParentId("");
    setSortOrder(0);
    setPublished(false);
    setTrLabel("");
    setEnLabel("");
    setError(null);
  }

  function choose(item: AdminMenuItem) {
    setId(item.id);
    setCode(item.code);
    setLocation(item.location === 2 ? "Footer" : "Header");
    setUrl(item.url);
    setExternal(item.isExternal);
    setNewTab(item.openInNewTab);
    setParentId(item.parentId ?? "");
    setSortOrder(item.sortOrder);
    setPublished(item.isPublished);
    setTrLabel(item.translations.find((text) => text.languageCode === "tr")?.label ?? "");
    setEnLabel(item.translations.find((text) => text.languageCode === "en")?.label ?? "");
    setError(null);
    setMessage(null);
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const response = await fetch(
        id ? `/api/admin/content/menu-items/${id}` : "/api/admin/content/menu-items",
        {
          method: id ? "PUT" : "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            code,
            location,
            url,
            isExternal: external,
            openInNewTab: external || newTab,
            parentId: parentId || null,
            sortOrder,
            isPublished: published,
            translations: [
              { languageCode: "tr", label: trLabel },
              ...(enLabel ? [{ languageCode: "en", label: enLabel }] : [])
            ]
          })
        }
      );
      if (!response.ok) throw new Error(await errorOf(response));
      await load();
      if (!id) reset();
      setMessage(id ? "Menü öğesi güncellendi." : "Menü öğesi oluşturuldu.");
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Kayıt tamamlanamadı.");
    } finally {
      setBusy(false);
    }
  }

  const locationValue = location === "Footer" ? 2 : 1;
  const parents = items.filter((item) =>
    item.id !== id &&
    item.location === locationValue
  );

  return <main className={styles.page}>
    <header className={styles.header}><div><p>EKİPHAN · İÇERİK</p><h1>Menü yönetimi</h1></div>
      <nav><a href="/admin">Dashboard</a><a href="/admin/content/pages">Sayfalar</a><a href="/">Siteye dön</a></nav></header>
    {error && <div className={styles.error} role="alert">{error}</div>}
    {message && <div className={styles.success} role="status">{message}</div>}
    <div className={styles.layout}>
      <section className={styles.panel}><div className={styles.title}><h2>Menü öğeleri</h2><button type="button" onClick={reset}>Yeni öğe</button></div>
        <div className={styles.list}>{items.map((item) =>
          <button type="button" key={item.id} onClick={() => choose(item)} data-selected={id === item.id}>
            <strong>{item.translations.find((text) => text.languageCode === "tr")?.label ?? item.code}</strong>
            <span>{item.location === 1 ? "Header" : "Footer"} · {item.url}</span><small>{item.isPublished ? "Yayında" : "Taslak"}</small>
          </button>)}</div></section>
      <form className={styles.form} onSubmit={save}>
        <div className={styles.row}><label>Kod<input required maxLength={100} value={code} onChange={(e) => setCode(e.target.value)} /></label>
          <label>Konum<select value={location} onChange={(e) => { setLocation(e.target.value); setParentId(""); }}><option>Header</option><option>Footer</option></select></label></div>
        <label>Türkçe etiket<input required maxLength={100} value={trLabel} onChange={(e) => setTrLabel(e.target.value)} /></label>
        <label>İngilizce etiket<input maxLength={100} value={enLabel} onChange={(e) => setEnLabel(e.target.value)} /></label>
        <label>Bağlantı<input required maxLength={2048} value={url} onChange={(e) => setUrl(e.target.value)} /><small>İç bağlantı `/sayfa/...`, anchor `#...`; dış bağlantı mutlak HTTPS olmalıdır.</small></label>
        <div className={styles.row}><label>Üst öğe<select value={parentId} onChange={(e) => setParentId(e.target.value)}><option value="">Yok</option>{parents.map((item) =>
          <option key={item.id} value={item.id}>{item.code}</option>)}</select></label>
          <label>Sıra<input type="number" value={sortOrder} onChange={(e) => setSortOrder(e.target.valueAsNumber)} /></label></div>
        <label className={styles.check}><input type="checkbox" checked={external} onChange={(e) => { setExternal(e.target.checked); if (e.target.checked) setNewTab(true); }} />Dış bağlantı</label>
        <label className={styles.check}><input type="checkbox" disabled={external} checked={external || newTab} onChange={(e) => setNewTab(e.target.checked)} />Yeni sekmede aç</label>
        <label className={styles.check}><input type="checkbox" checked={published} onChange={(e) => setPublished(e.target.checked)} />Yayında</label>
        <button disabled={busy}>Menü öğesini kaydet</button>
      </form>
    </div>
  </main>;
}

