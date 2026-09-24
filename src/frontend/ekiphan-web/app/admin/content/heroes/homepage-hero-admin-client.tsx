"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { AdminMediaPicker } from "../../../../components/admin-media-picker";
import type { AdminHomepageHero } from "../../../../lib/homepage-hero-types";
import styles from "./heroes.module.css";

const empty = {
  title: "", subtitle: "", primaryCtaLabel: "", primaryCtaUrl: "/katalog",
  secondaryCtaLabel: "", secondaryCtaUrl: ""
};

async function errorOf(response: Response) {
  try {
    const value = (await response.json()) as { detail?: string };
    return value.detail ?? "İşlem tamamlanamadı.";
  } catch { return "İşlem tamamlanamadı."; }
}

export function HomepageHeroAdminClient() {
  const [items, setItems] = useState<AdminHomepageHero[]>([]);
  const [id, setId] = useState<string | null>(null);
  const [desktopMediaId, setDesktopMediaId] = useState("");
  const [mobileMediaId, setMobileMediaId] = useState("");
  const [sortOrder, setSortOrder] = useState(0);
  const [published, setPublished] = useState(false);
  const [startsAt, setStartsAt] = useState("");
  const [endsAt, setEndsAt] = useState("");
  const [tr, setTr] = useState(empty);
  const [en, setEn] = useState(empty);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const load = useCallback(async () => {
    const response = await fetch("/api/admin/content/homepage-heroes", { cache: "no-store" });
    if (!response.ok) throw new Error(await errorOf(response));
    setItems((await response.json()) as AdminHomepageHero[]);
  }, []);
  useEffect(() => { void load().catch((error) => setNotice(error.message)); }, [load]);

  function reset() {
    setId(null); setDesktopMediaId(""); setMobileMediaId(""); setSortOrder(0);
    setPublished(false); setStartsAt(""); setEndsAt(""); setTr(empty); setEn(empty);
    setNotice(null);
  }

  function choose(item: AdminHomepageHero) {
    const text = (language: "tr" | "en") => {
      const value = item.translations.find((entry) => entry.languageCode === language);
      return value ? {
        title: value.title, subtitle: value.subtitle ?? "",
        primaryCtaLabel: value.primaryCtaLabel, primaryCtaUrl: value.primaryCtaUrl,
        secondaryCtaLabel: value.secondaryCtaLabel ?? "",
        secondaryCtaUrl: value.secondaryCtaUrl ?? ""
      } : empty;
    };
    setId(item.id); setDesktopMediaId(item.desktopMediaId ?? "");
    setMobileMediaId(item.mobileMediaId ?? ""); setSortOrder(item.sortOrder);
    setPublished(item.isPublished); setStartsAt(item.startsAt?.slice(0, 16) ?? "");
    setEndsAt(item.endsAt?.slice(0, 16) ?? ""); setTr(text("tr")); setEn(text("en"));
    setNotice(null);
  }

  async function save(event: FormEvent) {
    event.preventDefault(); setBusy(true); setNotice(null);
    const translation = (languageCode: "tr" | "en", value: typeof empty) => ({
      languageCode, title: value.title, subtitle: value.subtitle || null,
      primaryCtaLabel: value.primaryCtaLabel, primaryCtaUrl: value.primaryCtaUrl,
      secondaryCtaLabel: value.secondaryCtaLabel || null,
      secondaryCtaUrl: value.secondaryCtaLabel ? value.secondaryCtaUrl : null
    });
    try {
      const response = await fetch(
        id ? `/api/admin/content/homepage-heroes/${id}` : "/api/admin/content/homepage-heroes",
        {
          method: id ? "PUT" : "POST", headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            desktopMediaId: desktopMediaId || null, mobileMediaId: mobileMediaId || null,
            sortOrder, isPublished: published,
            startsAt: startsAt ? new Date(startsAt).toISOString() : null,
            endsAt: endsAt ? new Date(endsAt).toISOString() : null,
            translations: [translation("tr", tr), ...(en.title ? [translation("en", en)] : [])]
          })
        });
      if (!response.ok) throw new Error(await errorOf(response));
      await load(); if (!id) reset(); setNotice("Hero kaydedildi.");
    } catch (error) { setNotice(error instanceof Error ? error.message : "Kayıt başarısız."); }
    finally { setBusy(false); }
  }

  const fields = (language: "tr" | "en", value: typeof empty, setter: typeof setTr) => (
    <fieldset><legend>{language === "tr" ? "Türkçe" : "İngilizce"}</legend>
      <label>Başlık<input required={language === "tr"} maxLength={200} value={value.title}
        onChange={(e) => setter({ ...value, title: e.target.value })} /></label>
      <label>Alt başlık<textarea maxLength={500} value={value.subtitle}
        onChange={(e) => setter({ ...value, subtitle: e.target.value })} /></label>
      <div className={styles.row}><label>Birincil CTA<input required={language === "tr"} maxLength={100}
        value={value.primaryCtaLabel} onChange={(e) => setter({ ...value, primaryCtaLabel: e.target.value })} /></label>
        <label>CTA yolu<input required={language === "tr"} maxLength={2048}
          value={value.primaryCtaUrl} onChange={(e) => setter({ ...value, primaryCtaUrl: e.target.value })} /></label></div>
      <div className={styles.row}><label>İkincil CTA<input maxLength={100}
        value={value.secondaryCtaLabel} onChange={(e) => setter({ ...value, secondaryCtaLabel: e.target.value })} /></label>
        <label>İkincil CTA yolu<input maxLength={2048} value={value.secondaryCtaUrl}
          onChange={(e) => setter({ ...value, secondaryCtaUrl: e.target.value })} /></label></div>
    </fieldset>
  );

  return <main className={styles.page}>
    <header><div><p>EKİPHAN · ANA SAYFA</p><h1>Hero yönetimi</h1></div>
      <nav><a href="/admin">Dashboard</a><a href="/admin/media">Medya</a><a href="/">Site</a></nav></header>
    {notice && <div className={styles.notice} role="status">{notice}</div>}
    <div className={styles.layout}><section className={styles.list}><button onClick={reset}>Yeni hero</button>
      {items.map((item) => <button key={item.id} onClick={() => choose(item)}>
        <strong>{item.translations.find((text) => text.languageCode === "tr")?.title ?? item.id}</strong>
        <span>{item.isPublished ? "Yayında" : "Taslak"} · Sıra {item.sortOrder}</span>
      </button>)}</section>
      <form onSubmit={save}><div className={styles.row}>
        <AdminMediaPicker label="Masaüstü hero görseli" value={desktopMediaId}
          onChange={setDesktopMediaId} acceptedTypes={["Image"]} />
        <AdminMediaPicker label="Mobil hero görseli" value={mobileMediaId}
          onChange={setMobileMediaId} acceptedTypes={["Image"]} />
      </div><div className={styles.row}>
        <label>Başlangıç<input type="datetime-local" value={startsAt} onChange={(e) => setStartsAt(e.target.value)} /></label>
        <label>Bitiş<input type="datetime-local" value={endsAt} onChange={(e) => setEndsAt(e.target.value)} /></label>
        <label>Sıra<input type="number" value={sortOrder} onChange={(e) => setSortOrder(e.target.valueAsNumber)} /></label>
      </div>{fields("tr", tr, setTr)}{fields("en", en, setEn)}
      <label className={styles.check}><input type="checkbox" checked={published}
        onChange={(e) => setPublished(e.target.checked)} />Yayında</label>
      <button disabled={busy}>Hero kaydet</button></form></div>
  </main>;
}
