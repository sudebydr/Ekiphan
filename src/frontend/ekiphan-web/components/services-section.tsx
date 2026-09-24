"use client";

import Link from "next/link";
import { useState, type ReactNode } from "react";
import styles from "./services-section.module.css";

const services = [
  { title: "Proje Dan\u0131\u015fmanl\u0131\u011f\u0131", description: "\u0130htiyac\u0131n\u0131z\u0131 analiz ediyor, yat\u0131r\u0131m\u0131n\u0131za uygun yol haritas\u0131n\u0131 birlikte olu\u015fturuyoruz.", detail: "Kapasite, servis modeli, b\u00fct\u00e7e ve operasyon hedeflerini de\u011ferlendirerek projenin do\u011fru kapsamla ba\u015flamas\u0131n\u0131 sa\u011fl\u0131yoruz.", image: "/images/feature-consultation.png" },
  { title: "Mutfak Planlama", description: "Profesyonel mutfak ak\u0131\u015f\u0131n\u0131 verimli, g\u00fcvenli ve ergonomik bi\u00e7imde tasarl\u0131yoruz.", detail: "Haz\u0131rl\u0131k, pi\u015firme, servis ve bula\u015f\u0131k ak\u0131\u015flar\u0131n\u0131 alan\u0131n teknik ko\u015fullar\u0131na g\u00f6re planl\u0131yor; ekip yerle\u015fimini optimize ediyoruz.", image: "/images/feature-kitchen-triptych-v1.png" },
  { title: "\u00dcr\u00fcn Se\u00e7imi", description: "Do\u011fru marka ve ekipman\u0131 performans, b\u00fct\u00e7e ve kullan\u0131m yo\u011funlu\u011funa g\u00f6re se\u00e7iyoruz.", detail: "Geni\u015f \u00fcr\u00fcn portf\u00f6y\u00fcm\u00fczden kapasite, enerji verimlili\u011fi ve dayan\u0131kl\u0131l\u0131k ihtiya\u00e7lar\u0131na uygun alternatifler sunuyoruz.", image: "/images/industrial-kitchen-premium.png" },
  { title: "Kurulum ve Devreye Alma", description: "Teslimat, montaj ve \u00e7al\u0131\u015ft\u0131rma s\u00fcre\u00e7lerini uzman ekiplerle y\u00f6netiyoruz.", detail: "Saha koordinasyonundan testlere kadar t\u00fcm devreye alma ad\u0131mlar\u0131n\u0131 takip ediyor, ekibinize temel kullan\u0131m aktar\u0131m\u0131 sa\u011fl\u0131yoruz.", image: "/images/ekiphan-kitchen-hero.png" },
  { title: "Teknik Servis", description: "Operasyonun kesintisiz s\u00fcrmesi i\u00e7in h\u0131zl\u0131 ve g\u00fcvenilir teknik destek sa\u011fl\u0131yoruz.", detail: "Planl\u0131 bak\u0131m, ar\u0131za m\u00fcdahalesi ve performans kontrolleriyle ekipmanlar\u0131n\u0131z\u0131n uzun \u00f6m\u00fcrl\u00fc \u00e7al\u0131\u015fmas\u0131na destek oluyoruz.", image: "/images/feature-support-natural-v2.png" },
  { title: "Yedek Par\u00e7a Deste\u011fi", description: "Do\u011fru par\u00e7aya h\u0131zl\u0131 eri\u015fim sa\u011flayarak bak\u0131m s\u00fcrelerini minimuma indiriyoruz.", detail: "\u00dcr\u00fcn ve model bilgisine g\u00f6re uyumlu par\u00e7ay\u0131 belirliyor, tedarik ve de\u011fi\u015fim s\u00fcrecini teknik ekibimizle koordine ediyoruz.", image: "/images/feature-support-natural-v1.png" }
] as const;

function ServiceIcon() { return <svg viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="1.5"><path d="M5 3h10l4 4v14H5z"/><path d="M15 3v5h5M8 12h8M8 16h6"/></svg>; }

export function ServicesSection() {
  const [openIndex, setOpenIndex] = useState<number | null>(null);
  return <section className={styles.section} id="hizmetlerimiz" aria-labelledby="services-title">
    <header className={styles.heading}>
      <p className={styles.eyebrow}>{"NASIL DESTEK OLUYORUZ?"}</p>
      <h2 id="services-title">{"\u0130\u015fletmenizin ritmine uyum sa\u011flayan hizmet anlay\u0131\u015f\u0131."}</h2>
      <p className={styles.intro}>{"Ekiphan, geni\u015f \u00fcr\u00fcn portf\u00f6y\u00fc ve HoReCa sekt\u00f6r\u00fcne odaklanan yakla\u015f\u0131m\u0131yla arad\u0131\u011f\u0131n\u0131z ekipmana ula\u015fman\u0131z\u0131 kolayla\u015ft\u0131r\u0131r. Her projeyi kullan\u0131m alan\u0131, operasyon ve ihtiya\u00e7lar\u0131n\u0131z do\u011frultusunda ele al\u0131r\u0131z."}</p>
    </header>
    <div className={styles.grid}>
      {services.map((service, index) => { const isOpen = openIndex === index; return <article className={`${styles.card}${isOpen ? ` ${styles.open}` : ""}`} key={service.title}>
        <div className={styles.visual}><img src={service.image} alt="" /><span>{String(index + 1).padStart(2, "0")}</span></div>
        <div className={styles.copy}><span className={styles.icon}><ServiceIcon /></span><h3>{service.title}</h3><p>{service.description}</p>
          <div className={styles.detail} aria-hidden={!isOpen}><p>{service.detail}</p></div>
          <div className={styles.actions}><button type="button" aria-expanded={isOpen} onClick={() => setOpenIndex(isOpen ? null : index)}>{isOpen ? "Detay\u0131 Kapat" : "Hizmeti \u0130ncele"}<b aria-hidden="true">{isOpen ? "\u2212" : "\u25B8"}</b></button><Link href="/iletisim#contact-form">{"Teklif Al\u0131n"} <span aria-hidden="true">{"\u2192"}</span></Link></div>        </div>
      </article>; })}
    </div>
    <div className={styles.cta}><p>{"PROJEN\u0130Z\u0130 KONU\u015eALIM"}</p><strong>{"\u0130htiyac\u0131n\u0131z\u0131 anlat\u0131n, \u00e7\u00f6z\u00fcm\u00fc birlikte olu\u015ftural\u0131m."}</strong><Link href="/iletisim#contact-form">{"\u0130leti\u015fime Ge\u00e7in"}</Link></div>
  </section>;
}