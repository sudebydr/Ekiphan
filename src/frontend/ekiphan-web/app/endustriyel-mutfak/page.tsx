import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../../components/public-header";
import styles from "./industrial.module.css";

export const metadata: Metadata = {
  title: "Endüstriyel Mutfak",
  description:
    "Profesyonel mutfak projeleri için ekipman ve uygulama çözümleri.",
  alternates: { canonical: "/endustriyel-mutfak" }
};

const categories = [
  ["Hazırlık Ekipmanları", "Çalışma tezgahları, evyeler ve hazırlık masaları", "hazirlik"],
  ["Pişirme Ekipmanları", "Ocaklar, fırınlar, ızgaralar ve pişirme üniteleri", "pisirme"],
  ["Soğutma Ekipmanları", "Buzdolapları, şok soğutucular ve soğuk odalar", "sogutma"],
  ["Yıkama Sistemleri", "Bulaşık makineleri, yıkama üniteleri ve armatürler", "yikama"],
  ["Saklama Sistemleri", "Duvar dolapları, çekmeceli dolaplar ve raf sistemleri", "saklama"],
  ["Havalandırma", "Davlumbazlar, baca sistemleri ve filtre çözümleri", "havalandirma"]
] as const;

const benefits = [
  ["01", "Proje Danışmanlığı", "Alan, kapasite ve operasyonunuza uygun planlama."],
  ["02", "Doğru Ekipman", "Seçkin markalardan ihtiyacınıza uygun seçki."],
  ["03", "Kurulum ve Devreye Alma", "Uygulama, montaj ve kullanıma alma desteği."],
  ["04", "Satış Sonrası", "Bakım, teknik servis ve sürekli destek."]
] as const;

export default function IndustrialKitchenPage() {
  return (
    <main className={styles.page} data-public-page>
      <PublicHeader currentPath="/endustriyel-mutfak" />

      <section className={styles.hero}>
        <div className={styles.heroCopy}>
          <p>ENDÜSTRİYEL MUTFAK</p>
          <h1>Endüstriyel mutfaklarda doğru çözüm ortağınız.</h1>
          <span>
            Planlamadan ekipman seçimine, kurulumdan satış sonrası desteğe kadar
            profesyonel mutfak projelerinize uçtan uca değer katıyoruz.
          </span>
          <div>
            <Link href="/katalog">Ürünleri İncele</Link>
            <Link href="/iletisim">Projenizi Konuşalım</Link>
          </div>
        </div>
      </section>

      <section className={styles.categories}>
        <header>
          <p>EKİPMAN KATEGORİLERİ</p>
        </header>
        <div className={styles.categoryGrid}>
          {categories.map(([title, text, key]) => (
            <Link
              key={key}
              href={`/katalog?section=mutfak&category=${key}`}
              className={styles.categoryCard}
            >
              <span className={`${styles.categoryPhoto} ${styles[key]}`} />
              <h3>{title}</h3>
              <p>{text}</p>
              <b>
                Kategoriyi incele<i>→</i>
              </b>
            </Link>
          ))}
        </div>
      </section>

      <section className={styles.project}>
        <div className={styles.projectImage} />
        <div>
          <p>PROJEYE ÖZEL YAKLAŞIM</p>
          <span>
            Doğru yerleşim, doğru ekipman ve doğru operasyon akışıyla
            işletmenize özel profesyonel mutfaklar oluşturuyoruz.
          </span>
          <Link href="/iletisim">
            Teklif Al<i>→</i>
          </Link>
        </div>
      </section>

      <section className={styles.benefits}>
        {benefits.map(([number, title, text]) => (
          <article key={number}>
            <b>{number}</b>
            <h3>{title}</h3>
            <p>{text}</p>
          </article>
        ))}
      </section>
    </main>
  );
}