"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {
  ReactNode,
  useEffect,
  useMemo,
  useRef,
  useState
} from "react";
import type { AdminSession } from "./admin-session-guard";
import {
  findAdminNavigationItem,
  isAdminNavigationItemActive,
  navigationForPermissions
} from "./admin-navigation";
import styles from "./admin-shell.module.css";

const permissionLabels: Record<string, string> = {
  "catalog.manage": "Katalog yönetimi",
  "quotes.read": "Teklif görüntüleme",
  "quotes.manage": "Teklif yönetimi",
  "contacts.read": "İletişim görüntüleme",
  "contacts.manage": "İletişim yönetimi",
  "imports.manage": "İçe aktarma yönetimi",
  "imports.publish": "İçe aktarma yayınlama",
  "media.manage": "Medya yönetimi",
  "users.manage": "Kullanıcı ve yetki yönetimi",
  "content.manage": "İçerik yönetimi"
};

export function AdminShell({
  children,
  session
}: Readonly<{ children: ReactNode; session: AdminSession }>) {
  const pathname = usePathname();
  const router = useRouter();
  const [collapsed, setCollapsed] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [signingOut, setSigningOut] = useState(false);
  const drawerRef = useRef<HTMLElement>(null);
  const drawerButtonRef = useRef<HTMLButtonElement>(null);
  const groups = useMemo(
    () => navigationForPermissions(session.user.permissions),
    [session.user.permissions]
  );
  const current = findAdminNavigationItem(pathname, groups);
  const currentGroup = groups.find((group) =>
    group.items.some((item) => item.href === current?.href)
  );

  useEffect(() => {
    setDrawerOpen(false);
  }, [pathname]);

  useEffect(() => {
    if (!drawerOpen) return;
    const drawer = drawerRef.current;
    if (!drawer) return;

    const previousOverflow = document.body.style.overflow;
    const focusable = Array.from(
      drawer.querySelectorAll<HTMLElement>(
        'a[href], button:not([disabled]), summary, [tabindex]:not([tabindex="-1"])'
      )
    );
    document.body.style.overflow = "hidden";
    focusable[0]?.focus();

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setDrawerOpen(false);
        drawerButtonRef.current?.focus();
        return;
      }
      if (event.key !== "Tab" || focusable.length === 0) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [drawerOpen]);

  function toggleCollapsed() {
    setCollapsed((value) => !value);
  }

  async function signOut() {
    if (signingOut) return;
    setSigningOut(true);
    const controller = new AbortController();
    const timeout = window.setTimeout(() => controller.abort(), 3500);
    try {
      await fetch("/api/admin/auth/logout", {
        method: "POST",
        credentials: "same-origin",
        signal: controller.signal
      });
    } catch {
      // Do not leave the user stuck when the backend session endpoint is unavailable.
    } finally {
      window.clearTimeout(timeout);
      window.location.replace("/admin/login");
    }
  }
  return (
    <div className={styles.shell} data-collapsed={collapsed}>
      {drawerOpen && (
        <button
          type="button"
          className={styles.backdrop}
          aria-label="Menüyü kapat"
          onClick={() => setDrawerOpen(false)}
        />
      )}
      <aside
        className={styles.sidebar}
        data-open={drawerOpen}
        ref={drawerRef}
        aria-label="Admin navigasyonu"
      >
        <div className={styles.brandRow}>
          <Link href="/admin" className={styles.brand} aria-label="Ekiphan Admin">
            <span className={styles.brandMark} aria-hidden="true">E</span>
            <span className={styles.brandText}>
              <strong>Ekiphan</strong>
              <small>Yönetim Merkezi</small>
            </span>
          </Link>
          <button
            type="button"
            className={styles.desktopCollapse}
            onClick={toggleCollapsed}
            aria-label={collapsed ? "Menüyü genişlet" : "Menüyü daralt"}
            aria-expanded={!collapsed}
          >
            <span aria-hidden="true">{collapsed ? "›" : "‹"}</span>
          </button>
          <button
            type="button"
            className={styles.mobileClose}
            onClick={() => setDrawerOpen(false)}
            aria-label="Menüyü kapat"
          >
            ×
          </button>
        </div>

        <nav className={styles.navigation} aria-label="Yönetim bölümleri">
          {groups.map((group) => (
            <section className={styles.navGroup} key={group.label}>
              <h2>{group.label}</h2>
              {group.items.map((item) => {
                const active = isAdminNavigationItemActive(pathname, item.href);
                return (
                  <Link
                    href={item.href}
                    className={styles.navItem}
                    data-active={active}
                    aria-current={active ? "page" : undefined}
                    key={item.href}
                    title={collapsed ? item.label : undefined}
                  >
                    <span className={styles.navIcon} aria-hidden="true">
                      {item.label.slice(0, 1)}
                    </span>
                    <span className={styles.navCopy}>
                      <strong>{item.label}</strong>
                      <small>{item.description}</small>
                    </span>
                  </Link>
                );
              })}
            </section>
          ))}
        </nav>
        <Link href="/" className={styles.siteLink}>
          <span aria-hidden="true">↗</span>
          <span className={styles.navCopy}>Web sitesini görüntüle</span>
        </Link>
      </aside>

      <div className={styles.workspace}>
        <header className={styles.header}>
          <button
            type="button"
            className={styles.drawerButton}
            onClick={() => setDrawerOpen(true)}
            aria-label="Yönetim menüsünü aç"
            aria-expanded={drawerOpen}
            ref={drawerButtonRef}
          >
            <span aria-hidden="true">☰</span>
          </button>
          <div className={styles.pageIdentity}>
            <nav aria-label="Sayfa yolu" className={styles.breadcrumb}>
              <Link href="/admin">Yönetim</Link>
              {current?.href !== "/admin" && current && (
                <>
                  <span aria-hidden="true">/</span>
                  <span>{currentGroup?.label}</span>
                  <span aria-hidden="true">/</span>
                  <span aria-current="page">{current.label}</span>
                </>
              )}
            </nav>
            <strong>{current?.label ?? "Yönetim alanı"}</strong>
          </div>
          <details className={styles.userMenu}>
            <summary>
              <span className={styles.avatar} aria-hidden="true">
                {session.user.displayName.slice(0, 1).toLocaleUpperCase("tr-TR")}
              </span>
              <span className={styles.userSummary}>
                <strong>{session.user.displayName}</strong>
                <small>{session.user.email}</small>
              </span>
              <span aria-hidden="true">⌄</span>
            </summary>
            <div className={styles.userPanel}>
              <p>
                <strong>{session.user.displayName}</strong>
                <span>{session.user.email}</span>
              </p>
              {session.user.roles.length > 0 && (
                <p>
                  <small>Roller</small>
                  <span>{session.user.roles.join(", ")}</span>
                </p>
              )}
              <div className={styles.permissions}>
                <small>Yetkiler</small>
                <ul>
                  {session.user.permissions.map((permission) => (
                    <li key={permission}>
                      {permissionLabels[permission] ?? permission}
                    </li>
                  ))}
                </ul>
              </div>
              <div className={styles.userPanelFooter}>
              <button type="button" onClick={signOut} disabled={signingOut}>
                {signingOut ? "Çıkış yapılıyor…" : "Güvenli çıkış"}
              </button>
              </div>
            </div>
          </details>
        </header>
        <main className={styles.main} id="admin-page-content" tabIndex={-1}>
          {children}
        </main>
      </div>
    </div>
  );
}
