"use client";

import Link from "next/link";
import { useEffect } from "react";
import styles from "./status-page.module.css";

export default function ErrorPage({
  error,
  reset
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <main className={styles.page} data-public-page>
      <header className={styles.simpleTopbar}>
        <Link className={styles.brand} href="/">EKİPHAN</Link>
      </header>
      <section className={styles.hero}>
        <p>BEKLENMEYEN BİR DURUM</p>
        <h1>Sayfayı hazırlarken bir sorun oluştu.</h1>
        <span>İşlemi yeniden deneyebilir veya ana sayfaya dönebilirsiniz.</span>
        <div className={styles.actions}>
          <button type="button" onClick={reset}>Yeniden dene</button>
          <Link href="/">Ana sayfaya dön</Link>
        </div>
      </section>
    </main>
  );
}
