import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../../components/public-header";
import { PublicNavigation } from "../../components/public-navigation";
import { QuoteRequestClient } from "./quote-request-client";
import styles from "./quote.module.css";

export const metadata: Metadata = {
  title: "Teklif Listem",
  description:
    "Seçtiğiniz Ekiphan ürünleri için iletişim ve teklif talebi oluşturun.",
  robots: {
    index: false,
    follow: false,
    nocache: true
  }
};

function noticeUrl(): string | null {
  const value = process.env.NEXT_PUBLIC_KVKK_NOTICE_URL?.trim();
  if (!value) return null;
  if (value.startsWith("/") && !value.startsWith("//")) return value;
  try {
    const url = new URL(value);
    return url.protocol === "https:" ? url.href : null;
  } catch {
    return null;
  }
}

export default function QuoteListPage() {
  return (
    <main className={styles.page} data-public-page>
      <PublicHeader currentPath="/teklif-listem" />
      <section className={styles.hero}>
        <div>
          <p className={styles.eyebrow}>TEKLİF TALEBİ</p>
          <h1>
            Projenizi birlikte
            <br className={styles.desktopBreak} />
            planlayalım.
          </h1>
        </div>
        <p>
          Ürün adetlerini ve proje notlarınızı düzenleyin. Ekibimiz talebinizi
          inceleyip iletişim bilgileriniz üzerinden size ulaşacaktır.
        </p>
      </section>
      <QuoteRequestClient kvkkNoticeUrl={noticeUrl()} />
    </main>
  );
}
