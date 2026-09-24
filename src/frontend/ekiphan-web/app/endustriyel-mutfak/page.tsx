import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../../components/public-header";
import styles from "./industrial.module.css";

export const metadata: Metadata = { title: "End\u00fcstriyel Mutfak", description: "Profesyonel mutfak projeleri i\u00e7in ekipman ve uygulama \u00e7\u00f6z\u00fcmleri.", alternates: { canonical: "/endustriyel-mutfak" } };

const categories = [
  ["Haz\u0131rl\u0131k Ekipmanlar\u0131", "\u00c7al\u0131\u015fma tezgahlar\u0131, evyeler ve haz\u0131rl\u0131k masalar\u0131", "hazirlik"],
  ["Pi\u015firme Ekipmanlar\u0131", "Ocaklar, f\u0131r\u0131nlar, \u0131zgaralar ve pi\u015firme \u00fcniteleri", "pisirme"],
  ["So\u011futma Ekipmanlar\u0131", "Buzdolaplar\u0131, \u015fok so\u011futucular ve so\u011fuk odalar", "sogutma"],
  ["Y\u0131kama Sistemleri", "Bula\u015f\u0131k makineleri, y\u0131kama \u00fcniteleri ve armat\u00fcrler", "yikama"],
  ["Saklama Sistemleri", "Duvar dolaplar\u0131, \u00e7ekmeceli dolaplar ve raf sistemleri", "saklama"],
  ["Havaland\u0131rma", "Davlumbazlar, baca sistemleri ve filtre \u00e7\u00f6z\u00fcmleri", "havalandirma"]
] as const;

const benefits = [["01", "Proje Dan\u0131\u015fmanl\u0131\u011f\u0131", "Alan, kapasite ve operasyonunuza uygun planlama."], ["02", "Do\u011fru Ekipman", "Se\u00e7kin markalardan ihtiyac\u0131n\u0131za uygun se\u00e7ki."], ["03", "Kurulum ve Devreye Alma", "Uygulama, montaj ve kullan\u0131ma alma deste\u011fi."], ["04", "Sat\u0131\u015f Sonras\u0131", "Bak\u0131m, teknik servis ve s\u00fcrekli destek."]] as const;

export default function IndustrialKitchenPage() {
 return <main className={styles.page} data-public-page><PublicHeader currentPath="/endustriyel-mutfak" />
  <section className={styles.hero}>
  <div className={styles.heroCopy}>
    <p>{"END\u00dcSTR\u0130YEL MUTFAK"}</p>
    <h1>{"End\u00fcstriyel mutfaklarda do\u011fru \u00e7\u00f6z\u00fcm orta\u011f\u0131n\u0131z."}</h1>
    <span>{"Planlamadan ekipman se\u00e7imine, kurulumdan sat\u0131\u015f sonras\u0131 deste\u011fe kadar profesyonel mutfak projelerinize u\u00e7tan uca de\u011fer kat\u0131yoruz."}</span>
    <div>
      <Link href="/katalog">{"\u00dcr\u00fcnleri \u0130ncele"}</Link>
      <Link href="/iletisim">{"Projenizi Konu\u015fal\u0131m"}</Link>
    </div>
  </div>
</section>
  <section className={styles.categories}><header><p>{"EKİPMAN KATEGORİLERİ"}</p></header><div className={styles.categoryGrid}>{categories.map(([title,text,key])=><Link key={key} href={`/katalog?section=mutfak&category=${key}`} className={styles.categoryCard}><span className={`${styles.categoryPhoto} ${styles[key]}`} /><h3>{title}</h3><p>{text}</p><b>{"Kategoriyi incele"}<i>→</i></b></Link>)}</div></section>
  <section className={styles.project}><div className={styles.projectImage} /><div><p>{"PROJEYE ÖZEL YAKLAŞIM"}</p><span>{"Doğru yerleşim, doğru ekipman ve doğru operasyon akışıyla işletmenize özel profesyonel mutfaklar oluşturuyoruz."}</span><Link href="/iletisim">{"Teklif Al"}<i>→</i></Link></div></section>
  <section className={styles.benefits}>{benefits.map(([number,title,text])=><article key={number}><b>{number}</b><h3>{title}</h3><p>{text}</p></article>)}</section>
 </main>;
}