"use client";

import Link from "next/link";
import { KeyboardEvent, ReactNode, useRef, useState } from "react";
import styles from "./product-detail-sections.module.css";

type Specification = { id: string; name: string; value: string };
type SpecificationGroup = { name: string; items: Specification[] };
type Props = {
  productName: string;
  category: string;
  description: string;
  features: string[];
  specificationGroups: SpecificationGroup[];
  applicationAreas: string[];
  afterOverview?: ReactNode;
};

type IconName = "plan" | "support" | "install" | "parts" | "overview" | "specs" | "documents" | "applications" | "shield" | "performance" | "leaf" | "clean";

function Icon({ name }: { name: IconName }) {
  const common = { fill: "none", stroke: "currentColor", strokeWidth: 1.5, strokeLinecap: "round" as const, strokeLinejoin: "round" as const };
  const paths: Record<IconName, ReactNode> = {
    plan: <><path d="M4 20 20 4M7 4h13v13M4 12l8 8"/><path d="m8 8 2 2m2-6 8 8"/></>,
    support: <><path d="M4 13v-2a8 8 0 0 1 16 0v2"/><path d="M4 13a2 2 0 0 1 2-2h1v6H6a2 2 0 0 1-2-2v-2Zm16 0a2 2 0 0 0-2-2h-1v6h1a2 2 0 0 0 2-2v-2Z"/><path d="M17 17c0 2-2 3-5 3"/></>,
    install: <><path d="m14 6 4-4 4 4-4 4"/><path d="m2 22 7-7m5-5-4 4"/><circle cx="7" cy="17" r="3"/><path d="m15 15 2-2 4 4-2 2"/></>,
    parts: <><path d="m4 7 8-4 8 4-8 4-8-4Z"/><path d="M4 7v10l8 4 8-4V7M12 11v10"/></>,
    overview: <><circle cx="12" cy="12" r="8"/><path d="M12 8v8m-4-4h8"/></>,
    specs: <><path d="M4 7h10M18 7h2M4 17h2m4 0h10M8 4v6m0 4v6m8-9v6"/></>,
    documents: <><path d="M6 3h8l4 4v14H6V3Z"/><path d="M14 3v5h5M9 13h6m-6 4h6"/></>,
    applications: <><path d="M3 21h18M5 21V8l7-5 7 5v13M9 21v-7h6v7"/></>,
    shield: <><path d="M12 3 5 6v5c0 4.5 2.8 8 7 10 4.2-2 7-5.5 7-10V6l-7-3Z"/><path d="m9 12 2 2 4-5"/></>,
    performance: <><path d="M4 17a8 8 0 1 1 16 0"/><path d="m12 13 4-4M7 17h10"/></>,
    leaf: <><path d="M20 4C10 4 5 9 5 16c4 1 11 0 15-12Z"/><path d="M4 20c3-5 7-8 12-11"/></>,
    clean: <><path d="m12 3 1.2 3.8L17 8l-3.8 1.2L12 13l-1.2-3.8L7 8l3.8-1.2L12 3ZM5 14l.8 2.2L8 17l-2.2.8L5 20l-.8-2.2L2 17l2.2-.8L5 14Zm14-1 .8 2.2L22 16l-2.2.8L19 19l-.8-2.2L16 16l2.2-.8L19 13Z"/></>
  };
  return <svg viewBox="0 0 24 24" aria-hidden="true" {...common}>{paths[name]}</svg>;
}

const services = [
  { number: "01", icon: "plan" as const, title: "Proje desteği", text: "Profesyonel proje danışmanlığı ve mutfak planlama desteği." },
  { number: "02", icon: "support" as const, title: "Teknik destek", text: "Satış sonrası teknik destek ve hızlı çözüm desteği." },
  { number: "03", icon: "install" as const, title: "Kurulum", text: "Profesyonel montaj ve devreye alma hizmeti." },
  { number: "04", icon: "parts" as const, title: "Yedek parça", text: "Orijinal yedek parça desteği ve hızlı tedarik." }
];

const tabs = [
  { id: "overview", icon: "overview" as const, label: "Genel bakış" },
  { id: "specs", icon: "specs" as const, label: "Teknik özellikler" },
  { id: "documents", icon: "documents" as const, label: "Dokümanlar" },
  { id: "applications", icon: "applications" as const, label: "Uygulama alanları" }
] as const;

export function ProductDetailSections({ productName, category, description, features, specificationGroups, applicationAreas, afterOverview }: Props) {
  const [activeTab, setActiveTab] = useState<(typeof tabs)[number]["id"]>("overview");
  const tabRefs = useRef<Array<HTMLButtonElement | null>>([]);

  function onTabKeyDown(event: KeyboardEvent<HTMLButtonElement>, index: number) {
    let next = index;
    if (event.key === "ArrowRight") next = (index + 1) % tabs.length;
    else if (event.key === "ArrowLeft") next = (index - 1 + tabs.length) % tabs.length;
    else if (event.key === "Home") next = 0;
    else if (event.key === "End") next = tabs.length - 1;
    else return;
    event.preventDefault();
    setActiveTab(tabs[next].id);
    tabRefs.current[next]?.focus();
  }

  const featureIcons: IconName[] = ["shield", "performance", "leaf", "clean"];
  const featureDescriptions = [
    "Kaliteli bileşenleriyle yoğun kullanımlarda uzun ömürlü performans sunar.",
    "Profesyonel operasyonların temposuna uyumlu, güvenilir çalışma kapasitesi.",
    "Verimli kullanım yapısıyla işletme kaynaklarını dengeli kullanmaya yardımcı olur.",
    "Hijyen standartlarını destekleyen, bakımı ve temizliği kolay yüzeyler."
  ];
  const displayFeatures = [...features, "Üstün dayanıklılık", "Yüksek performans", "Enerji verimliliği", "Kolay temizlik"].filter((item, index, list) => list.indexOf(item) === index).slice(0, 4);

  return <><section className={styles.details} aria-label={`${productName} ürün detayları`}>
      <div className={styles.tabList} role="tablist" aria-label="Ürün detay bölümleri">
        {tabs.map((tab, index) => <button key={tab.id} ref={(node) => { tabRefs.current[index] = node; }} type="button" role="tab" id={`product-tab-${tab.id}`} aria-selected={activeTab === tab.id} aria-controls="product-detail-panel" tabIndex={activeTab === tab.id ? 0 : -1} onClick={() => setActiveTab(tab.id)} onKeyDown={(event) => onTabKeyDown(event, index)}>
          <Icon name={tab.icon}/><span>{tab.label}</span>
        </button>)}
      </div>

      <div className={styles.panel} role="tabpanel" id="product-detail-panel" aria-labelledby={`product-tab-${activeTab}`} tabIndex={0}>
        {activeTab === "overview" && <><div className={styles.overview}>
          <div className={styles.intro}><span className={styles.kicker}>{category}</span><h2>Profesyonel mutfaklar için maksimum performans</h2><i aria-hidden="true"/><p>{description}</p><p>Ekiphan’ın proje deneyimi ve satış sonrası desteğiyle profesyonel kullanıma yönelik güvenilir bir ürün çözümü sunar.</p></div>
          <ol className={styles.features}>{displayFeatures.map((feature, index) => <li key={feature}><span>{String(index + 1).padStart(2, "0")}</span><Icon name={featureIcons[index]}/><div><h3>{feature}</h3><p>{featureDescriptions[index]}</p></div></li>)}</ol>
        </div>{afterOverview}</>}

        {activeTab === "specs" && <div className={styles.specifications}>
          <header><span className={styles.kicker}>Ürün verileri</span><h2>Teknik özellikler</h2></header>
          {specificationGroups.length ? <div className={styles.specGrid}>{specificationGroups.map((group) => <section key={group.name}><h3>{group.name}</h3><dl>{group.items.map((item) => <div key={item.id}><dt>{item.name}</dt><dd>{item.value}</dd></div>)}</dl></section>)}</div> : <p className={styles.empty}>Teknik özellikler ürün kaydı tamamlandığında burada yayınlanacaktır.</p>}
        </div>}

        {activeTab === "documents" && <div className={styles.documents}>
          <header><span className={styles.kicker}>Dosyalar</span><h2>Teknik dokümanlar</h2></header>
          <div><Link href="/iletisim"><span>PDF</span><strong>Teknik föy<small>Proje ekibimizden talep edin</small></strong><b>Talep et →</b></Link><Link href="/iletisim"><span>PDF</span><strong>Kullanım ve bakım bilgisi<small>Teknik ekibimizden talep edin</small></strong><b>Talep et →</b></Link><Link href="/kataloglar"><span>PDF</span><strong>Ürün kataloğu<small>Güncel katalogları inceleyin</small></strong><b>İncele →</b></Link></div>
        </div>}

        {activeTab === "applications" && <div className={styles.applications}>
          <header><span className={styles.kicker}>Uygulama</span><h2>Bu ürün nerelerde kullanılır?</h2></header>
          <div>{applicationAreas.map((area, index) => <article key={area}><span>{String(index + 1).padStart(2, "0")}</span><strong>{area}</strong></article>)}</div>
        </div>}
      </div>
    </section>
    <section className={styles.services} aria-label="Ekiphan hizmet avantajları">
      {services.map((service) => <article key={service.number}>
        <div className={styles.serviceTop}><Icon name={service.icon}/><span>{service.number}</span></div>
        <h2>{service.title}</h2><p>{service.text}</p>
      </article>)}
    </section>

  </>;
}
