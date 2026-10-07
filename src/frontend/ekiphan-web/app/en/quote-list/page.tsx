import type { Metadata } from "next";
import { PublicHeader } from "../../../components/public-header";
import { QuoteRequestClient } from "../../teklif-listem/quote-request-client";
import styles from "../../teklif-listem/quote.module.css";

export const metadata: Metadata = { title: "Request a Quote | Ekiphan", description: "Prepare a quote request for your selected Ekiphan products.", robots: { index: false, follow: false } };

function noticeUrl(): string | null {
  const value = process.env.NEXT_PUBLIC_KVKK_NOTICE_URL?.trim();
  if (!value) return null;
  if (value.startsWith("/") && !value.startsWith("//")) return value;
  try { const url = new URL(value); return url.protocol === "https:" ? url.href : null; } catch { return null; }
}

export default function EnglishQuoteListPage() {
  return <main className={styles.page} data-public-page>
    <PublicHeader currentPath="/en/quote-list" />
    <section className={styles.hero}><div><p className={styles.eyebrow}>REQUEST A QUOTE</p><h1>Let’s plan your<br className={styles.desktopBreak} /> project together.</h1></div><p>Adjust product quantities and project notes. Our team will review your request and contact you using the details provided.</p></section>
    <QuoteRequestClient kvkkNoticeUrl={noticeUrl()} locale="en" />
  </main>;
}
