"use client";

import {
  createContext,
  ReactNode,
  useContext,
  useEffect,
  useMemo,
  useState
} from "react";
import { usePathname, useRouter } from "next/navigation";
import { AdminShell } from "./admin-shell";
import {
  adminDashboardPermissions,
  canAccessAdminPath
} from "./admin-navigation";
import styles from "./admin-shell.module.css";

export type AdminSessionUser = {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  permissions: string[];
};

export type AdminSession = {
  authenticated: true;
  idleTimeoutMinutes: number;
  user: AdminSessionUser;
};

type AdminSessionContextValue = {
  session: AdminSession;
  hasPermission: (permission: string) => boolean;
};

type GuardState =
  | { status: "checking" }
  | { status: "authenticated"; session: AdminSession }
  | { status: "forbidden" }
  | { status: "error"; message: string };

const AdminSessionContext = createContext<AdminSessionContextValue | null>(null);
const activityRefreshMilliseconds = 5 * 60 * 1000;

export function useAdminSession(): AdminSessionContextValue {
  const value = useContext(AdminSessionContext);
  if (!value) {
    throw new Error(
      "useAdminSession must be used inside the protected admin layout."
    );
  }

  return value;
}

export function AdminSessionGuard({
  children
}: Readonly<{ children: ReactNode }>) {
  const pathname = usePathname();
  const router = useRouter();
  const isLoginPage = pathname === "/admin/login";
  const [state, setState] = useState<GuardState>({ status: "checking" });
  const [retryKey, setRetryKey] = useState(0);

  useEffect(() => {
    if (isLoginPage) {
      return;
    }

    const controller = new AbortController();
    setState({ status: "checking" });
    void fetch("/api/admin/auth/session", {
      cache: "no-store",
      credentials: "same-origin",
      signal: controller.signal
    })
      .then(async (response) => {
        if (response.status === 401) {
          const returnUrl = `${window.location.pathname}${window.location.search}`;
          router.replace(
            `/admin/login?returnUrl=${encodeURIComponent(returnUrl)}`
          );
          return;
        }
        if (response.status === 403) {
          setState({ status: "forbidden" });
          return;
        }
        if (!response.ok) {
          setState({
            status: "error",
            message: "Yönetim servisine şu anda ulaşılamıyor."
          });
          return;
        }

        const session = (await response.json()) as AdminSession;
        if (
          !session.authenticated ||
          !session.user ||
          !Number.isInteger(session.idleTimeoutMinutes) ||
          session.idleTimeoutMinutes < 1
        ) {
          setState({
            status: "error",
            message: "Admin oturumu doğrulama cevabı geçersiz."
          });
          return;
        }

        setState({ status: "authenticated", session });
      })
      .catch((reason: unknown) => {
        if (reason instanceof DOMException && reason.name === "AbortError") {
          return;
        }
        setState({
          status: "error",
          message: "Admin oturumu doğrulanırken bağlantı hatası oluştu."
        });
      });

    return () => controller.abort();
  }, [isLoginPage, pathname, retryKey, router]);

  useEffect(() => {
    if (isLoginPage || state.status !== "authenticated") {
      return;
    }

    const controller = new AbortController();
    let lastServerRefresh = Date.now();
    let idleTimer: ReturnType<typeof setTimeout>;

    const expireLocalSession = () => {
      setState({ status: "checking" });
      void fetch("/api/admin/auth/logout", {
        method: "POST",
        credentials: "same-origin",
        signal: controller.signal
      }).finally(() => {
        if (!controller.signal.aborted) {
          router.replace("/admin/login");
        }
      });
    };

    const resetIdleTimer = () => {
      clearTimeout(idleTimer);
      idleTimer = setTimeout(
        expireLocalSession,
        state.session.idleTimeoutMinutes * 60 * 1000
      );
    };

    const refreshServerSession = async () => {
      try {
        const response = await fetch("/api/admin/auth/session", {
          cache: "no-store",
          credentials: "same-origin",
          signal: controller.signal
        });
        if (response.status === 401) {
          setState({ status: "checking" });
          router.replace("/admin/login");
        } else if (response.status === 403) {
          setState({ status: "forbidden" });
        } else if (!response.ok) {
          setState({
            status: "error",
            message: "Admin oturumu yenilenemedi."
          });
        }
      } catch (reason: unknown) {
        if (!(reason instanceof DOMException && reason.name === "AbortError")) {
          setState({
            status: "error",
            message: "Admin oturumu yenilenirken bağlantı hatası oluştu."
          });
        }
      }
    };

    const recordActivity = () => {
      resetIdleTimer();
      const now = Date.now();
      if (now - lastServerRefresh >= activityRefreshMilliseconds) {
        lastServerRefresh = now;
        void refreshServerSession();
      }
    };

    resetIdleTimer();
    window.addEventListener("pointerdown", recordActivity, { passive: true });
    window.addEventListener("keydown", recordActivity);
    window.addEventListener("touchstart", recordActivity, { passive: true });

    return () => {
      controller.abort();
      clearTimeout(idleTimer);
      window.removeEventListener("pointerdown", recordActivity);
      window.removeEventListener("keydown", recordActivity);
      window.removeEventListener("touchstart", recordActivity);
    };
  }, [isLoginPage, router, state.status]);

  const contextValue = useMemo<AdminSessionContextValue | null>(() => {
    if (state.status !== "authenticated") {
      return null;
    }

    return {
      session: state.session,
      hasPermission: (permission: string) =>
        state.session.user.permissions.includes(permission)
    };
  }, [state]);

  if (isLoginPage) {
    return children;
  }
  if (state.status === "checking") {
    return (
      <main className={styles.guardPage} aria-busy="true">
        <div className={styles.guardCard} role="status">
          <span className={styles.guardLogo} aria-hidden="true">E</span>
          <div className={styles.guardSkeleton} />
          <div className={styles.guardSkeleton} />
          <span className={styles.srOnly}>Admin oturumu doğrulanıyor…</span>
        </div>
      </main>
    );
  }
  if (state.status === "forbidden") {
    return <UnauthorizedState dashboardAvailable={false} />;
  }
  if (state.status === "error") {
    return (
      <main className={styles.guardPage}>
        <div className={styles.guardCard}>
        <span className={styles.guardLogo} aria-hidden="true">!</span>
        <h1>Oturum doğrulanamadı</h1>
        <p role="alert">{state.message}</p>
        <div className={styles.guardActions}>
          <button type="button" onClick={() => setRetryKey((value) => value + 1)}>
            Yeniden dene
          </button>
          <a href="/admin/login">Giriş sayfasına dön</a>
        </div>
        </div>
      </main>
    );
  }

  if (!canAccessAdminPath(pathname, state.session.user.permissions)) {
    return (
      <UnauthorizedState
        dashboardAvailable={adminDashboardPermissions.some((permission) =>
          state.session.user.permissions.includes(permission)
        )}
      />
    );
  }

  return (
    <AdminSessionContext.Provider value={contextValue}>
      <AdminShell session={state.session}>{children}</AdminShell>
    </AdminSessionContext.Provider>
  );
}

function UnauthorizedState({
  dashboardAvailable
}: Readonly<{ dashboardAvailable: boolean }>) {
  return (
    <main className={styles.guardPage}>
      <div className={styles.guardCard}>
        <span className={styles.guardLogo} aria-hidden="true">!</span>
        <h1>Yetkisiz erişim</h1>
        <p>Bu yönetim alanını görüntülemek için yetkiniz bulunmuyor.</p>
        <div className={styles.guardActions}>
          {dashboardAvailable && <a href="/admin">Dashboard’a dön</a>}
          <a href="/">Siteye dön</a>
        </div>
      </div>
    </main>
  );
}
