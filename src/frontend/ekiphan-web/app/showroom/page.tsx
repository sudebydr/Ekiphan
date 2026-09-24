import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../../components/public-header";
import { PublicNavigation } from "../../components/public-navigation";
import { CatalogApiError, getContentPage } from "../../lib/catalog-api";
import type { PublicContentPage } from "../../lib/content-types";
import { defaultSocialImage } from "../../lib/social-metadata";
import styles from "./showroom.module.css";
import catalogStyles from "../katalog/catalog.module.css";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Showroom",
  description: "Ekiphan showroom bilgileri ve yayımlanmış dijital tur bağlantısı.",
  alternates: { canonical: "/showroom" },
  openGraph: {
    type: "website",
    locale: "tr_TR",
    title: "Ekiphan Showroom",
    description:
      "Ekiphan showroom bilgileri ve yayımlanmış dijital tur bağlantısı.",
    images: defaultSocialImage ? [defaultSocialImage] : undefined,
    url: "/showroom"
  }
};

function safeTourUrl(): string | null {
  const value = process.env.SHOWROOM_TOUR_URL?.trim();
  if (!value) return null;

  try {
    const parsed = new URL(value);
    return parsed.protocol === "https:" &&
      parsed.hostname.length > 0 &&
      parsed.username.length === 0 &&
      parsed.password.length === 0
      ? parsed.href
      : null;
  } catch {
    return null;
  }
}

export default async function ShowroomPage() {
  let content: PublicContentPage | null = null;
  let error: string | null = null;

  try {
    content = await getContentPage("showroom");
  } catch (caught) {
    if (!(caught instanceof CatalogApiError && caught.status === 404)) {
      error =
        caught instanceof CatalogApiError
          ? caught.message
          : "Showroom bilgileri şu anda görüntülenemiyor.";
    }
  }

  const paragraphs = content?.body
    .split(/\r?\n{2,}/)
    .map((item) => item.trim())
    .filter(Boolean) ?? [];
  const tourUrl = safeTourUrl();

  return (
    <main style={{ width: '100%', maxWidth: '100vw', overflowX: 'hidden', backgroundColor: '#f8f4ee', margin: 0, padding: 0 }} data-public-page>
      <PublicHeader currentPath="/showroom" />

      <section style={{ position: 'relative', width: '100%', minHeight: '100vh', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', margin: 0, padding: 0, borderRadius: 0, border: 'none', overflow: 'hidden' }}>
        <video 
          src="/videos/ekiphan-showroom.mp4" 
          autoPlay 
          loop 
          muted 
          playsInline 
          style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', objectFit: 'cover', zIndex: 0, borderRadius: 0 }}
        />
      </section>

      {content && (
        <section className={styles.content} aria-label="Showroom bilgileri" style={{ maxWidth: '86rem', margin: '0 auto', padding: '4rem 2rem' }}>
          <div className={styles.copy}>
            {paragraphs.map((paragraph, index) => (
              <p key={`${index}-${paragraph.slice(0, 24)}`}>{paragraph}</p>
            ))}
          </div>
          <aside className={styles.tour}>
            <p className={styles.eyebrow}>DİJİTAL ZİYARET</p>
            <h2>Showroom turu</h2>
            {tourUrl ? (
              <>
                <p>
                  Tur, doğrulanmış harici sağlayıcı üzerinde yeni sekmede açılır.
                </p>
                <a
                  href={tourUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  Showroom'u Gör <span aria-hidden="true">↗</span>
                </a>
              </>
            ) : (
              <p>
                Onaylı dijital tur bağlantısı henüz yapılandırılmamıştır.
              </p>
            )}
          </aside>
        </section>
      )}
    </main>
  );
}
