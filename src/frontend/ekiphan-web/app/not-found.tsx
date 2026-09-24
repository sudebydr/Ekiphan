import Link from "next/link";
import styles from "./status-page.module.css";
import { PublicHeader } from "../components/public-header";

export default function NotFound() {
  return <main className={styles.page} data-public-page>
    <PublicHeader />
    <section className={styles.hero}>
      <div className={styles.backdrop} aria-hidden="true" />
      <div className={styles.content}>
        <span className={styles.code}>404</span>
        <h1>{"Sayfa Bulunamad\u0131"}</h1>
        <p>{"Arad\u0131\u011f\u0131n\u0131z sayfa ta\u015f\u0131nm\u0131\u015f, kald\u0131r\u0131lm\u0131\u015f ya da ba\u011flant\u0131 hatal\u0131 olabilir."}</p>
        <div className={styles.actions}>
          <Link className={styles.primaryAction} href="/"><span aria-hidden="true">{"\u2302"}</span> <span>{"Anasayfaya D\u00f6n"}</span></Link>
          <Link className={styles.secondaryAction} href="/katalog"><span aria-hidden="true">{"\u25a3"}</span> <span>{"\u00dcr\u00fcnleri Ke\u015ffet"}</span></Link>
        </div>

      </div>
    </section>
    <section className={styles.assurances} aria-label={"Ekiphan avantajlar\u0131"}>
      <div><i aria-hidden="true">{"\u2726"}</i><span><strong>Profesyonel Kalite</strong><small>{"Uzun \u00f6m\u00fcrl\u00fc kullan\u0131m"}</small></span></div>
      <div><i aria-hidden="true">{"\u2713"}</i><span><strong>{"G\u00fcvenli Al\u0131\u015fveri\u015f"}</strong><small>SSL ile korunur</small></span></div>
      <div><i aria-hidden="true">{"\u25c8"}</i><span><strong>{"H\u0131zl\u0131 Destek"}</strong><small>{"7/24 yan\u0131n\u0131zday\u0131z"}</small></span></div>
      <div><i aria-hidden="true">{"\u25a3"}</i><span><strong>{"Kurumsal \u00c7\u00f6z\u00fcmler"}</strong><small>{"Projelere \u00f6zel teklif"}</small></span></div>
    </section>
  </main>;
}