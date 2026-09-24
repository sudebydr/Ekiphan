import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../../components/public-header";
import styles from "./about.module.css";

export const metadata: Metadata = { title: "Hakk\u0131m\u0131zda", description: "Ekiphan'\u0131n profesyonel mutfak \u00e7\u00f6z\u00fcmlerine yakla\u015f\u0131m\u0131, de\u011ferleri ve \u00e7al\u0131\u015fma ilkeleri." };

const values = [
  ["G\u00fcvenilirlik", "S\u00fcrecin her a\u015famas\u0131nda a\u00e7\u0131k ileti\u015fim ve tutarl\u0131 teslim."],
  ["Uzmanl\u0131k", "Sekt\u00f6r bilgisini projenizin ihtiya\u00e7lar\u0131yla birle\u015ftiririz."],
  ["\u00c7\u00f6z\u00fcm Odakl\u0131l\u0131k", "Her alan i\u00e7in uygulanabilir, verimli bir yol haritas\u0131 kurar\u0131z."],
  ["S\u00fcrd\u00fcr\u00fclebilir \u0130\u015f Birli\u011fi", "Teslimin \u00f6tesinde, uzun soluklu destek sunar\u0131z."]
] as const;

export default function AboutPage() { return <main className={styles.page} data-public-page>
  <PublicHeader currentPath="/hakkimizda" />
  <section className={styles.storyHero} aria-labelledby="about-story-title"><div className={styles.storyPanel}><p className={styles.eyebrow}>{"EK\u0130PHAN\u2019I TANIYIN"}</p><h1 id="about-story-title">{"K\u00f6klerimizden Gelece\u011fe"}</h1><p>{"Ekiphan; profesyonel mutfaklar\u0131n ihtiya\u00e7lar\u0131n\u0131 dinleyen, do\u011fru ekipman\u0131 do\u011fru projeyle bulu\u015fturan bir \u00e7\u00f6z\u00fcm orta\u011f\u0131d\u0131r."}</p><p>{"Planlamadan tedarike, kurulumdan sat\u0131\u015f sonras\u0131 deste\u011fe kadar her ad\u0131mda deneyimimizi i\u015fletmenizin ritmiyle birle\u015ftiriyoruz."}</p></div></section>
  <section className={styles.identity} aria-labelledby="identity-title"><p className={styles.sectionLabel}>{"B\u0130Z K\u0130M\u0130Z?"}</p><div><h2 id="identity-title">{"\u0130htiyac\u0131 anlayan, \u00e7\u00f6z\u00fcm\u00fc birlikte tasarlayan ekip."}</h2><p>{"Otel, restoran, kafe ve end\u00fcstriyel mutfak projelerinde g\u00fc\u00e7l\u00fc marka se\u00e7kisini operasyonel bilgiyle bir araya getiriyoruz. Her projenin kendine \u00f6zg\u00fc kullan\u0131m al\u0131\u015fkanl\u0131\u011f\u0131n\u0131, kapasitesini ve hedefini dikkate al\u0131yoruz."}</p><p>{"Amac\u0131m\u0131z yaln\u0131zca ekipman tedarik etmek de\u011fil; uzun \u00f6m\u00fcrl\u00fc, verimli ve g\u00fcven veren \u00e7al\u0131\u015fma alanlar\u0131 olu\u015fturmakt\u0131r."}</p></div></section>
  <section className={styles.missionVision} aria-label="Misyon ve vizyon"><article className={styles.mission}><p className={styles.eyebrow}>{"M\u0130SYONUMUZ"}</p><h2>{"\u0130\u015fletmeler i\u00e7in i\u015flevsel, g\u00fcvenilir \u00e7\u00f6z\u00fcmler \u00fcretmek."}</h2><p>{"Profesyonel ekipman bilgisini, ihtiyaca uygun planlama ve g\u00fcvenilir uygulamayla bulu\u015ftururuz."}</p></article><article className={styles.vision}><p className={styles.eyebrow}>{"V\u0130ZYONUMUZ"}</p><h2>{"Her projede kal\u0131c\u0131 de\u011fer yaratan \u00e7\u00f6z\u00fcm orta\u011f\u0131 olmak."}</h2><p>{"HoReCa d\u00fcnyas\u0131nda se\u00e7kin markalar ve g\u00fc\u00e7l\u00fc hizmet anlay\u0131\u015f\u0131yla s\u00fcrd\u00fcr\u00fclebilir i\u015f birlikleri kurar\u0131z."}</p></article></section>
  <section className={styles.values} aria-labelledby="values-title"><header><p className={styles.eyebrow}>{"DE\u011eERLER\u0130M\u0130Z"}</p><h2 id="values-title">{"Her projede ayn\u0131 \u00e7al\u0131\u015fma ilkeleri."}</h2></header><div className={styles.valuesGrid}>{values.map(([title,text],index)=><article key={title}><span>{String(index+1).padStart(2,"0")}</span><h3>{title}</h3><p>{text}</p></article>)}</div></section>
  <section className={styles.contactCta} aria-labelledby="contact-title"><p className={styles.eyebrow}>{"B\u0130RL\u0130KTE PLANLAYALIM"}</p><h2 id="contact-title">{"\u0130htiyac\u0131n\u0131z\u0131 konu\u015fal\u0131m, do\u011fru \u00e7\u00f6z\u00fcm\u00fc birlikte kural\u0131m."}</h2><Link href="/iletisim#contact-form">{"\u0130leti\u015fime ge\u00e7in"}</Link></section>
</main>; }