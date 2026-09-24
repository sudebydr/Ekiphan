"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type {
  AdminDashboard,
  AdminDashboardProduct,
  AdminDashboardQuote
} from "../../lib/admin-dashboard-types";
import type { AdminSeoHealth } from "../../lib/admin-seo-types";
import type { SiteSettingsDto } from "../../lib/public-settings";
import { useAdminSession } from "./admin-session-guard";
import styles from "./admin-dashboard.module.css";

const quoteStatusLabels: Record<string, string> = {
  New: "Yeni",
  Reviewing: "İnceleniyor",
  Contacted: "İletişime geçildi",
  Preparing: "Hazırlanıyor",
  Sent: "Gönderildi",
  Won: "Kazanıldı",
  Lost: "Kaybedildi",
  Archived: "Arşivlendi"
};

type DashboardFailure = {
  kind: "unauthorized" | "error";
  message: string;
};

export function AdminDashboardClient() {
  const { session } = useAdminSession();
  const [dashboard, setDashboard] = useState<AdminDashboard | null>(null);
  const [seoHealth, setSeoHealth] = useState<AdminSeoHealth | null>(null);
  const [settings, setSettings] = useState<SiteSettingsDto | null>(null);
  const [failure, setFailure] = useState<DashboardFailure | null>(null);
  const [refreshing, setRefreshing] = useState(false);
  const activeRequest = useRef<AbortController | null>(null);

  const quickActions = useMemo(() => {
    const granted = new Set(session.user.permissions);
    return [
      { label: "Ürünleri Yönet", href: "/admin/catalog/products", permissions: ["catalog.manage"] },
      { label: "Ürün İçe Aktar", href: "/admin/imports", permissions: ["imports.manage"] },
      { label: "Medya Yükle", href: "/admin/media", permissions: ["media.manage"] },
      { label: "Teklif Taleplerini Gör", href: "/admin/quotes", permissions: ["quotes.read", "quotes.manage"] },
      { label: "İletişim Taleplerini Gör", href: "/admin/contact", permissions: ["contacts.read", "contacts.manage"] },
      { label: "Sayfa İçeriklerini Yönet", href: "/admin/content/pages", permissions: ["content.manage"] }
    ].filter((item) => item.permissions.some((permission) => granted.has(permission)));
  }, [session.user.permissions]);

  const loadDashboard = useCallback(async () => {
    if (activeRequest.current) return;
    const controller = new AbortController();
    activeRequest.current = controller;
    setRefreshing(true);
    setFailure(null);
    
    let attempt = 0;
    const maxAttempts = 3;

    while (attempt < maxAttempts) {
      if (controller.signal.aborted) break;

      try {
        const [dashRes, seoRes, setRes] = await Promise.allSettled([
          fetch("/api/admin/dashboard", { cache: "no-store", credentials: "same-origin", signal: controller.signal }),
          fetch("/api/admin/seo/summary", { cache: "no-store", credentials: "same-origin", signal: controller.signal }),
          fetch("/api/admin/settings", { cache: "no-store", credentials: "same-origin", signal: controller.signal })
        ]);

        if (controller.signal.aborted || activeRequest.current !== controller) return;

        let isTransientError = false;

        if (dashRes.status === "fulfilled") {
          const response = dashRes.value;
          if (response.status === 401) {
            const returnUrl = `${window.location.pathname}${window.location.search}`;
            window.location.assign(`/admin/login?returnUrl=${encodeURIComponent(returnUrl)}`);
            return;
          }
          if (response.status === 403) {
            setFailure({ kind: "unauthorized", message: "Dashboard özetini görüntüleme yetkiniz bulunmuyor." });
            setDashboard(null);
            return;
          }
          if (!response.ok) {
            if (response.status >= 500 || response.status === 502 || response.status === 503) {
              isTransientError = true;
            } else {
              setFailure({ kind: "error", message: "Dashboard verileri şu anda alınamıyor." });
              break;
            }
          } else {
            setDashboard(await response.json());
            if (seoRes.status === "fulfilled" && seoRes.value.ok) setSeoHealth(await seoRes.value.json());
            if (setRes.status === "fulfilled" && setRes.value.ok) setSettings(await setRes.value.json());
            break;
          }
        } else {
          isTransientError = true;
        }

        if (isTransientError) {
          attempt++;
          if (attempt >= maxAttempts) {
            setFailure({ kind: "error", message: "Dashboard servisine bağlanılamadı." });
            break;
          }
          await new Promise(resolve => setTimeout(resolve, 600));
          if (controller.signal.aborted || activeRequest.current !== controller) return;
          continue;
        }
      } catch (reason: unknown) {
        if (controller.signal.aborted || activeRequest.current !== controller) return;
        attempt++;
        if (attempt >= maxAttempts) {
          setFailure({ kind: "error", message: "Dashboard servisine bağlanılamadı." });
          break;
        }
        await new Promise(resolve => setTimeout(resolve, 600));
        if (controller.signal.aborted || activeRequest.current !== controller) return;
        continue;
      }
    }

    if (activeRequest.current === controller) {
      activeRequest.current = null;
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    void loadDashboard();
    return () => {
      activeRequest.current?.abort();
      activeRequest.current = null;
    };
  }, [loadDashboard]);

  if (!dashboard && refreshing) return <DashboardSkeleton />;

  if (!dashboard && failure?.kind === "unauthorized") {
    return (
      <section className={styles.centerState} role="alert">
        <span aria-hidden="true">!</span>
        <h1>Dashboard yetkisi gerekli</h1>
        <p>{failure.message}</p>
        <Link href="/">Web sitesine dön</Link>
      </section>
    );
  }

  if (!dashboard && failure) {
    return (
      <section className={styles.centerState} role="alert">
        <span aria-hidden="true">!</span>
        <h1>Dashboard yüklenemedi</h1>
        <p>{failure.message}</p>
        <button type="button" onClick={() => void loadDashboard()}>Yeniden dene</button>
      </section>
    );
  }

  if (!dashboard) return null;

  const metrics = [
    dashboard.products && { label: "Toplam Ürün", value: dashboard.products.total, detail: "Katalogdaki toplam ürün" },
    dashboard.products && { label: "Aktif Ürün", value: dashboard.products.published, detail: "Müşterilere görünür ürün" },
    dashboard.products && { label: "Pasif Ürün", value: dashboard.products.unpublished, detail: "Yayında olmayan ürün" },
    dashboard.totalCategories !== null && { label: "Toplam Kategori", value: dashboard.totalCategories, detail: "Katalog sınıflandırması" },
    dashboard.totalBrands !== null && { label: "Toplam Marka", value: dashboard.totalBrands, detail: "Katalogdaki markalar" },
    dashboard.quotes && { label: "Yeni Teklif", value: dashboard.quotes.new, detail: "İşlem bekleyen talepler" },
    dashboard.quotes && { label: "Toplam Teklif", value: dashboard.quotes.total, detail: "Tüm teklif talepleri" },
    dashboard.newContactRequests !== null && { label: "Yeni İletişim", value: dashboard.newContactRequests, detail: "Yanıt bekleyen talepler" }
  ].filter((item): item is { label: string; value: number; detail: string } => Boolean(item));

  return (
    <div className={styles.dashboard}>
      <header className={styles.intro} aria-labelledby="dashboard-title">
        <div>
          <p className={styles.eyebrow}>EKİPHAN · YÖNETİM</p>
          <h1 id="dashboard-title">Yönetim Özeti</h1>
          <p>Yönetim paneline hoş geldiniz. Yetkiniz olan operasyonların güncel durumunu görüntüleyin.</p>
        </div>
        <div className={styles.refreshArea}>
          <div className={styles.updateMeta}><span>Son güncelleme</span><strong>{formatDateTime(dashboard.generatedAt)}</strong></div>
        </div>
      </header>

      {failure?.kind === "error" && (
        <div className={styles.inlineError} role="alert">
          <div>
            <strong>Veriler tam yenilenemedi.</strong>
            <span>Son başarılı özet gösterilmeye devam ediyor.</span>
          </div>
          <button type="button" onClick={() => void loadDashboard()}>Yeniden dene</button>
        </div>
      )}

      {metrics.length > 0 && (
        <div className={styles.metrics}>
          {metrics.map((metric) => (
            <article className={styles.metricCard} key={metric.label}>
              <span>{metric.label}</span>
              <strong>{metric.value.toLocaleString("tr-TR")}</strong>
              <small>{metric.detail}</small>
            </article>
          ))}
        </div>
      )}

      <div className={styles.mainGrid}>
        <div className={styles.mainCol}>
          {dashboard.recentProducts && (
            <DashboardListSection
              title="Son Güncellenen Ürünler"
              href="/admin/catalog/products"
              empty="Henüz ürün kaydı bulunmuyor. Ürün kataloğunu oluşturmaya başlayabilirsiniz."
            >
              <ProductList items={dashboard.recentProducts} />
            </DashboardListSection>
          )}

          <section className={styles.panel}>
            <div className={styles.sectionHeading}>
              <div><h2>Operasyon Durumu</h2></div>
            </div>
            <div className={styles.opsGrid}>
              {dashboard.products && (
                <div className={styles.opsItem}>
                  <span>Taslak / Pasif Ürünler</span>
                  <strong>{dashboard.products.unpublished}</strong>
                  <Link href="/admin/catalog/products">Ürünlere Git →</Link>
                </div>
              )}
              {dashboard.products && (
                <div className={styles.opsItem}>
                  <span>Görseli Olmayan Ürün</span>
                  <strong>{dashboard.products.missingGalleryImage}</strong>
                  <Link href="/admin/catalog/products">Ürünlere Git →</Link>
                </div>
              )}
              {seoHealth && (
                <>
                  <div className={styles.opsItem}>
                    <span>Eksik SEO Başlığı</span>
                    <strong>{seoHealth.missingMetaTitle}</strong>
                    <Link href="/admin/seo">SEO Yönetimi →</Link>
                  </div>
                  <div className={styles.opsItem}>
                    <span>Eksik SEO Açıklaması</span>
                    <strong>{seoHealth.missingMetaDescription}</strong>
                    <Link href="/admin/seo">SEO Yönetimi →</Link>
                  </div>
                  <div className={styles.opsItem}>
                    <span>Eksik İngilizce İçerik</span>
                    <strong>{dashboard.products?.missingEnglishTranslation ?? seoHealth.missingEnglish}</strong>
                    <Link href="/admin/catalog/products">Ürünlere Git →</Link>
                  </div>
                  <div className={styles.opsItem}>
                    <span>Noindex İçerik</span>
                    <strong>{seoHealth.noIndex}</strong>
                    <Link href="/admin/seo">SEO Yönetimi →</Link>
                  </div>
                </>
              )}
            </div>
            {!dashboard.products && !seoHealth && (
              <EmptyState message="Operasyon verisi şu anda yüklenemedi." compact />
            )}
          </section>
          
          {dashboard.recentQuotes && (
            <DashboardListSection
              title="Son Teklif Talepleri"
              href="/admin/quotes"
              empty="Henüz teklif talebi bulunmuyor."
            >
              <QuoteList items={dashboard.recentQuotes} />
            </DashboardListSection>
          )}
        </div>

        <div className={styles.sideCol}>
          <section className={styles.panel}>
            <div className={styles.sectionHeading}>
              <div><h2>Hızlı İşlemler</h2></div>
            </div>
            {quickActions.length > 0 ? (
              <nav className={styles.quickActions}>
                {quickActions.map((item) => (
                  <Link href={item.href} key={item.href}>
                    <strong>{item.label}</strong>
                    <span aria-hidden="true">→</span>
                  </Link>
                ))}
              </nav>
            ) : (
              <EmptyState message="Yetkiniz dahilinde işlem bulunmuyor." compact />
            )}
          </section>

          <section className={styles.panel}>
            <div className={styles.sectionHeading}>
              <div><h2>SEO Özeti</h2></div>
              <Link href="/admin/seo" className={styles.sectionLink}>Tümü</Link>
            </div>
            {seoHealth ? (
              <ul className={styles.summaryList}>
                <li><span>Indexlenebilir Kayıt</span><strong>{seoHealth.totalIndexable}</strong></li>
                <li><span>Kopya SEO Başlığı</span><strong>{seoHealth.duplicateMetaTitles}</strong></li>
                <li><span>Kopya URL (Slug)</span><strong>{seoHealth.duplicateSlugs}</strong></li>
                <li><span>Eksik OG Görseli</span><strong>{seoHealth.missingOpenGraphImage}</strong></li>
              </ul>
            ) : (
              <EmptyState message="SEO özeti yüklenemedi." compact />
            )}
          </section>

          <section className={styles.panel}>
            <div className={styles.sectionHeading}>
              <div><h2>Site Ayarları</h2></div>
              <Link href="/admin/settings" className={styles.sectionLink}>Düzenle</Link>
            </div>
            {settings ? (
              <ul className={styles.summaryList}>
                <li><span>Telefon</span><strong>{settings.phone ? "Tamamlandı" : "Eksik"}</strong></li>
                <li><span>E-posta</span><strong>{settings.contactEmail ? "Tamamlandı" : "Eksik"}</strong></li>
                <li><span>Adres</span><strong>{settings.factoryAddress || settings.showroomAddress || settings.warehouseAddress ? "Tamamlandı" : "Eksik"}</strong></li>
                <li><span>Sosyal Medya</span><strong>{settings.instagramUrl || settings.linkedInUrl || settings.youTubeUrl ? "Tamamlandı" : "Eksik"}</strong></li>
              </ul>
            ) : (
              <EmptyState message="Ayarlar özeti yüklenemedi." compact />
            )}
          </section>
        </div>
      </div>
    </div>
  );
}

function DashboardSkeleton() {
  return (
    <div className={styles.dashboard} aria-busy="true" role="status">
      <span className={styles.srOnly}>Yönetim özeti yükleniyor…</span>
      <div className={styles.skeletonHero} />
      <div className={styles.skeletonMetrics}>
        {Array.from({ length: 4 }, (_, index) => (
          <div key={index} />
        ))}
      </div>
      <div className={styles.mainGrid}>
        <div className={styles.mainCol}>
          <div className={styles.skeletonPanelLg} />
          <div className={styles.skeletonPanelLg} />
        </div>
        <div className={styles.sideCol}>
          <div className={styles.skeletonPanelSm} />
          <div className={styles.skeletonPanelSm} />
          <div className={styles.skeletonPanelSm} />
        </div>
      </div>
    </div>
  );
}

function DashboardListSection({
  title,
  href,
  empty,
  children
}: Readonly<{
  title: string;
  href: string;
  empty: string;
  children: React.ReactNode;
}>) {
  const child = children as React.ReactElement<{ items?: unknown[] }>;
  const isEmpty = child.props.items?.length === 0;
  return (
    <section className={styles.panel}>
      <div className={styles.sectionHeading}>
        <div><h2>{title}</h2></div>
        <Link href={href} className={styles.sectionLink}>Tümünü gör <span aria-hidden="true">→</span></Link>
      </div>
      {isEmpty ? <EmptyState message={empty} /> : children}
    </section>
  );
}

function QuoteList({ items }: Readonly<{ items: AdminDashboardQuote[] }>) {
  return (
    <div className={styles.tableScroller}>
      <table className={styles.quoteTable}>
        <thead>
          <tr>
            <th scope="col">Teklif No</th>
            <th scope="col">Firma</th>
            <th scope="col">Tarih</th>
            <th scope="col">Ürün</th>
            <th scope="col">Durum</th>
            <th scope="col"><span className={styles.srOnly}>İşlem</span></th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td><strong>{item.requestNumber}</strong></td>
              <td>{item.companyName}</td>
              <td><time dateTime={item.createdAt}>{formatDateTime(item.createdAt)}</time></td>
              <td>{item.itemCount.toLocaleString("tr-TR")}</td>
              <td><span className={styles.statusBadge} data-status={item.status}>{quoteStatusLabels[item.status] ?? item.status}</span></td>
              <td><Link href={`/admin/quotes?quoteId=${encodeURIComponent(item.id)}`}>Detayı Gör</Link></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function ProductList({ items }: Readonly<{ items: AdminDashboardProduct[] }>) {
  return (
    <ul className={styles.recordList}>
      {items.map((item) => (
        <li key={item.id}>
          <span className={styles.recordMark} aria-hidden="true">Ü</span>
          <span><strong>{item.name}</strong><small>{item.sku}</small></span>
          <span className={styles.recordMeta}>
            <strong>{item.isPublished ? "Yayında" : "Pasif"}</strong>
            <small>{formatDateTime(item.updatedAt)}</small>
          </span>
        </li>
      ))}
    </ul>
  );
}

function EmptyState({ message, compact = false }: Readonly<{ message: string; compact?: boolean }>) {
  return <p className={styles.empty} data-compact={compact}>{message}</p>;
}

function formatDateTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "—";
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(date);
}
