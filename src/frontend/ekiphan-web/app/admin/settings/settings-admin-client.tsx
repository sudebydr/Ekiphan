"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import styles from "./settings.module.css";

interface SiteSettingsDto {
  phone: string;
  fax: string;
  contactEmail: string;
  showroomAddress: string;
  factoryAddress: string;
  warehouseAddress: string;
  instagramUrl: string | null;
  linkedInUrl: string | null;
  youTubeUrl: string | null;
  companyTitle: string;
  companySlogan: string;
  showroomTourUrl: string | null;
  rowVersion: string;
}

async function readError(response: Response) {
  try {
    const value = (await response.json()) as { detail?: string };
    return value.detail ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function SettingsAdminClient() {
  const [data, setData] = useState<SiteSettingsDto | null>(null);
  const [draft, setDraft] = useState<SiteSettingsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch("/api/admin/settings", {
        cache: "no-store",
      });
      if (!response.ok) throw new Error(await readError(response));
      const settings = (await response.json()) as SiteSettingsDto;
      setData(settings);
      setDraft(settings);
    } catch (reason) {
      setError(
        reason instanceof Error ? reason.message : "Site ayarları alınamadı."
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const update = (field: keyof SiteSettingsDto, value: string | null) => {
    setDraft((prev) => (prev ? { ...prev, [field]: value } : null));
    setMessage(null);
  };

  async function save(event: FormEvent) {
    event.preventDefault();
    if (!draft) return;
    setBusy(true);
    setError(null);
    try {
      const response = await fetch(`/api/admin/settings`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(draft),
      });
      if (!response.ok) throw new Error(await readError(response));
      const updated = (await response.json()) as SiteSettingsDto;
      setData(updated);
      setDraft(updated);
      setMessage("Site ayarları başarıyla kaydedildi.");
    } catch (reason) {
      setError(
        reason instanceof Error ? reason.message : "Ayarlar kaydedilemedi."
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <div>
          <p>EKİPHAN · AYARLAR</p>
          <h1>Site Ayarları</h1>
          <span>Tüm site genelindeki iletişim ve şirket bilgilerini yönetin.</span>
        </div>
        <a href="/admin">Dashboard</a>
      </header>

      {error && (
        <div className={styles.error} role="alert">
          {error}
        </div>
      )}
      {message && (
        <div className={styles.success} role="status">
          {message}
        </div>
      )}

      {loading && (
        <div className={styles.form}>
          <div className={styles.section}>
            <div className={styles.skeleton} />
          </div>
        </div>
      )}

      {!loading && draft && (
        <form className={styles.form} onSubmit={save}>
          <div className={styles.section}>
            <h2>Kurumsal Kimlik</h2>
            <div className={styles.grid}>
              <div className={styles.field}>
                <label>Şirket Unvanı</label>
                <input
                  required
                  maxLength={150}
                  value={draft.companyTitle}
                  onChange={(e) => update("companyTitle", e.target.value)}
                />
              </div>
              <div className={styles.field}>
                <label>Şirket Sloganı</label>
                <input
                  required
                  maxLength={150}
                  value={draft.companySlogan}
                  onChange={(e) => update("companySlogan", e.target.value)}
                />
              </div>
            </div>
          </div>

          <div className={styles.section}>
            <h2>İletişim Bilgileri</h2>
            <div className={styles.grid}>
              <div className={styles.field}>
                <label>Genel İletişim Telefonu</label>
                <input
                  required
                  maxLength={30}
                  value={draft.phone}
                  onChange={(e) => update("phone", e.target.value)}
                />
              </div>
              <div className={styles.field}>
                <label>Faks Numarası</label>
                <input
                  required
                  maxLength={30}
                  value={draft.fax}
                  onChange={(e) => update("fax", e.target.value)}
                />
              </div>
              <div className={styles.field}>
                <label>İletişim E-posta Adresi</label>
                <input
                  required
                  type="email"
                  maxLength={254}
                  value={draft.contactEmail}
                  onChange={(e) => update("contactEmail", e.target.value)}
                />
              </div>
            </div>
          </div>

          <div className={styles.section}>
            <h2>Adres Bilgileri</h2>
            <div className={styles.gridFull}>
              <div className={styles.field}>
                <label>Merkez Showroom Adresi</label>
                <textarea
                  required
                  rows={2}
                  maxLength={500}
                  value={draft.showroomAddress}
                  onChange={(e) => update("showroomAddress", e.target.value)}
                />
              </div>
              <div className={styles.field}>
                <label>Fabrika Adresi</label>
                <textarea
                  required
                  rows={2}
                  maxLength={500}
                  value={draft.factoryAddress}
                  onChange={(e) => update("factoryAddress", e.target.value)}
                />
              </div>
              <div className={styles.field}>
                <label>Depo Adresi</label>
                <textarea
                  required
                  rows={2}
                  maxLength={500}
                  value={draft.warehouseAddress}
                  onChange={(e) => update("warehouseAddress", e.target.value)}
                />
              </div>
            </div>
          </div>

          <div className={styles.section}>
            <h2>Sosyal Medya & Bağlantılar</h2>
            <div className={styles.grid}>
              <div className={styles.field}>
                <label>Instagram URL</label>
                <input
                  type="url"
                  maxLength={500}
                  value={draft.instagramUrl || ""}
                  placeholder="https://instagram.com/..."
                  onChange={(e) => update("instagramUrl", e.target.value || null)}
                />
              </div>
              <div className={styles.field}>
                <label>LinkedIn URL</label>
                <input
                  type="url"
                  maxLength={500}
                  value={draft.linkedInUrl || ""}
                  placeholder="https://linkedin.com/..."
                  onChange={(e) => update("linkedInUrl", e.target.value || null)}
                />
              </div>
              <div className={styles.field}>
                <label>YouTube URL</label>
                <input
                  type="url"
                  maxLength={500}
                  value={draft.youTubeUrl || ""}
                  placeholder="https://youtube.com/..."
                  onChange={(e) => update("youTubeUrl", e.target.value || null)}
                />
              </div>
              <div className={styles.field}>
                <label>Showroom Tur URL</label>
                <input
                  type="url"
                  maxLength={1000}
                  value={draft.showroomTourUrl || ""}
                  placeholder="https://..."
                  onChange={(e) => update("showroomTourUrl", e.target.value || null)}
                />
              </div>
            </div>
          </div>

          <div className={styles.footer}>
            <button type="submit" className={styles.button} disabled={busy}>
              {busy ? "Kaydediliyor..." : "Değişiklikleri Kaydet"}
            </button>
          </div>
        </form>
      )}
    </main>
  );
}
