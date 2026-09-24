"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import styles from "./users.module.css";

type AdminUser = {
  id: string;
  email: string;
  displayName: string;
  isActive: boolean;
  accessFailedCount: number;
  lockoutEnd: string | null;
  lastLoginAt: string | null;
  permissions: string[];
};

type AdminUserPage = {
  items: AdminUser[];
  page: number;
  pageSize: number;
  totalCount: number;
};

const pageSize = 50;

const permissions = [
  ["catalog.manage", "Katalog yönetimi"],
  ["quotes.manage", "Teklif yönetimi"],
  ["imports.manage", "İçe aktarma yönetimi"],
  ["imports.publish", "İçe aktarma yayınlama"],
  ["media.manage", "Medya yönetimi"],
  ["content.manage", "Kurumsal içerik yönetimi"],
  ["users.manage", "Kullanıcı ve izin yönetimi"]
] as const;

async function errorOf(response: Response) {
  try {
    const value = (await response.json()) as { detail?: string };
    return value.detail ?? "İşlem tamamlanamadı.";
  } catch {
    return "İşlem tamamlanamadı.";
  }
}

export function UserAdminClient() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [email, setEmail] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [active, setActive] = useState(true);
  const [grants, setGrants] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async (requestedPage: number) => {
    const response = await fetch(
      `/api/admin/users?page=${requestedPage}&pageSize=${pageSize}`,
      { cache: "no-store" }
    );
    if (!response.ok) throw new Error(await errorOf(response));
    const result = (await response.json()) as AdminUserPage;
    setUsers(result.items);
    setPage(result.page);
    setTotalCount(result.totalCount);
  }, []);

  useEffect(() => {
    void load(1).catch((reason: unknown) =>
      setError(reason instanceof Error ? reason.message : "Kullanıcılar alınamadı.")
    );
  }, [load]);

  function reset() {
    setSelectedId(null);
    setEmail("");
    setDisplayName("");
    setPassword("");
    setActive(true);
    setGrants([]);
    setError(null);
    setMessage(null);
  }

  function select(user: AdminUser) {
    setSelectedId(user.id);
    setEmail(user.email);
    setDisplayName(user.displayName);
    setPassword("");
    setActive(user.isActive);
    setGrants(user.permissions);
    setError(null);
    setMessage(null);
  }

  function toggle(permission: string) {
    setGrants((current) =>
      current.includes(permission)
        ? current.filter((item) => item !== permission)
        : [...current, permission]
    );
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const response = await fetch(
        selectedId ? `/api/admin/users/${selectedId}` : "/api/admin/users",
        {
          method: selectedId ? "PUT" : "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            email,
            displayName,
            password: selectedId ? null : password,
            isActive: active,
            permissions: grants
          })
        }
      );
      if (!response.ok) throw new Error(await errorOf(response));
      await load(selectedId ? page : 1);
      if (!selectedId) reset();
      setMessage(selectedId ? "Kullanıcı güncellendi." : "Kullanıcı oluşturuldu.");
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Kayıt tamamlanamadı.");
    } finally {
      setBusy(false);
    }
  }

  async function resetPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedId) return;
    const data = new FormData(event.currentTarget);
    setBusy(true);
    setError(null);
    try {
      const response = await fetch(`/api/admin/users/${selectedId}/password`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ password: data.get("newPassword") })
      });
      if (!response.ok) throw new Error(await errorOf(response));
      event.currentTarget.reset();
      setMessage("Parola yenilendi.");
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Parola yenilenemedi.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <div><p>EKİPHAN · ADMIN</p><h1>Kullanıcılar ve izinler</h1></div>
        <nav><a href="/admin">Dashboard</a><a href="/">Siteye dön</a></nav>
      </header>
      {error && <div className={styles.error} role="alert">{error}</div>}
      {message && <div className={styles.success} role="status">{message}</div>}
      <div className={styles.layout}>
        <section className={styles.panel}>
          <div className={styles.panelTitle}>
            <h2>Yöneticiler</h2>
            <button type="button" onClick={reset}>Yeni kullanıcı</button>
          </div>
          <div className={styles.list}>
            {users.map((user) => (
              <button
                type="button"
                key={user.id}
                onClick={() => select(user)}
                data-selected={selectedId === user.id}
              >
                <strong>{user.displayName}</strong>
                <span>{user.email}</span>
                <small>{user.isActive ? "Aktif" : "Pasif"} · {user.permissions.length} izin</small>
              </button>
            ))}
          </div>
          {totalCount > pageSize && (
            <div className={styles.pagination}>
              <button
                type="button"
                disabled={page === 1 || busy}
                onClick={() => void load(page - 1)}
              >
                Önceki
              </button>
              <span>{page} / {Math.ceil(totalCount / pageSize)}</span>
              <button
                type="button"
                disabled={page * pageSize >= totalCount || busy}
                onClick={() => void load(page + 1)}
              >
                Sonraki
              </button>
            </div>
          )}
        </section>
        <section className={styles.panel}>
          <h2>{selectedId ? "Kullanıcıyı düzenle" : "Yeni kullanıcı"}</h2>
          <form className={styles.form} onSubmit={save}>
            <label>Görünen ad<input required maxLength={150} value={displayName} onChange={(e) => setDisplayName(e.target.value)} /></label>
            <label>E-posta<input required type="email" maxLength={254} value={email} onChange={(e) => setEmail(e.target.value)} /></label>
            {!selectedId && <label>İlk parola<input required type="password" minLength={14} maxLength={256} autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} /><small>En az 14 karakter; büyük/küçük harf, rakam ve sembol.</small></label>}
            <fieldset><legend>İzinler</legend>{permissions.map(([value, label]) =>
              <label className={styles.check} key={value}><input type="checkbox" checked={grants.includes(value)} onChange={() => toggle(value)} />{label}</label>)}</fieldset>
            <label className={styles.check}><input type="checkbox" checked={active} onChange={(e) => setActive(e.target.checked)} />Aktif kullanıcı</label>
            <button disabled={busy || grants.length === 0}>Kaydet</button>
          </form>
          {selectedId && <form className={styles.form} onSubmit={resetPassword}>
            <h3>Parolayı yenile</h3>
            <label>Yeni parola<input name="newPassword" required type="password" minLength={14} maxLength={256} autoComplete="new-password" /></label>
            <button disabled={busy}>Parolayı değiştir</button>
          </form>}
        </section>
      </div>
    </main>
  );
}
