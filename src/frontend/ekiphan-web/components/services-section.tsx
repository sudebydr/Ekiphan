"use client";

import Link from "next/link";
import { useState } from "react";
import styles from "./services-section.module.css";

const servicesTr = [
  {
    title: "Proje Danışmanlığı",
    description:
      "İhtiyacınızı analiz ediyor, yatırımınıza uygun yol haritasını birlikte oluşturuyoruz.",
    detail:
      "Kapasite, servis modeli, bütçe ve operasyon hedeflerini değerlendirerek projenin doğru kapsamla başlamasını sağlıyoruz.",
    image: "/images/feature-consultation.png",
  },
  {
    title: "Mutfak Planlama",
    description:
      "Profesyonel mutfak akışını verimli, güvenli ve ergonomik biçimde tasarlıyoruz.",
    detail:
      "Hazırlık, pişirme, servis ve bulaşık akışlarını alanın teknik koşullarına göre planlıyor; ekip yerleşimini optimize ediyoruz.",
    image: "/images/feature-kitchen-triptych-v1.png",
  },
  {
    title: "Ürün Seçimi",
    description:
      "Doğru marka ve ekipmanı performans, bütçe ve kullanım yoğunluğuna göre seçiyoruz.",
    detail:
      "Geniş ürün portföyümüzden kapasite, enerji verimliliği ve dayanıklılık ihtiyaçlarına uygun alternatifler sunuyoruz.",
    image: "/images/industrial-kitchen-premium.png",
  },
  {
    title: "Kurulum ve Devreye Alma",
    description:
      "Teslimat, montaj ve çalıştırma süreçlerini uzman ekiplerle yönetiyoruz.",
    detail:
      "Saha koordinasyonundan testlere kadar tüm devreye alma adımlarını takip ediyor, ekibinize temel kullanım aktarımı sağlıyoruz.",
    image: "/images/ekiphan-kitchen-hero.png",
  },
  {
    title: "Teknik Servis",
    description:
      "Operasyonun kesintisiz sürmesi için hızlı ve güvenilir teknik destek sağlıyoruz.",
    detail:
      "Planlı bakım, arıza müdahalesi ve performans kontrolleriyle ekipmanlarınızın uzun ömürlü çalışmasına destek oluyoruz.",
    image: "/images/feature-support-natural-v2.png",
  },
  {
    title: "Yedek Parça Desteği",
    description:
      "Doğru parçaya hızlı erişim sağlayarak bakım sürelerini minimuma indiriyoruz.",
    detail:
      "Ürün ve model bilgisine göre uyumlu parçayı belirliyor, tedarik ve değişim sürecini teknik ekibimizle koordine ediyoruz.",
    image: "/images/feature-support-natural-v1.png",
  },
] as const;
const servicesEn = [
  { title: "Project Consultancy", description: "We assess your needs and build a roadmap suited to your investment.", detail: "We evaluate capacity, service model, budget and operational goals so your project starts with the right scope.", image: "/images/feature-consultation.png" },
  { title: "Kitchen Planning", description: "We design efficient, safe and ergonomic professional kitchen workflows.", detail: "We plan preparation, cooking, service and dishwashing flows around the technical conditions of your space and optimize equipment placement.", image: "/images/feature-kitchen-triptych-v1.png" },
  { title: "Equipment Selection", description: "We select the right brands and equipment for performance, budget and usage intensity.", detail: "We present options from our broad portfolio to match your capacity, energy efficiency and durability requirements.", image: "/images/industrial-kitchen-premium.png" },
  { title: "Installation & Commissioning", description: "Our specialists manage delivery, installation and commissioning.", detail: "We oversee every commissioning step, from site coordination and testing to basic user guidance for your team.", image: "/images/ekiphan-kitchen-hero.png" },
  { title: "Technical Service", description: "We provide fast, dependable technical support to keep your operation running.", detail: "Scheduled maintenance, repair response and performance checks help your equipment run reliably for longer.", image: "/images/feature-support-natural-v2.png" },
  { title: "Spare Parts Support", description: "We minimize maintenance downtime with quick access to the right parts.", detail: "We identify compatible parts by product and model, then coordinate supply and replacement with our technical team.", image: "/images/feature-support-natural-v1.png" },
] as const;

function ServiceIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
    >
      <path d="M5 3h10l4 4v14H5z" />
      <path d="M15 3v5h5M8 12h8M8 16h6" />
    </svg>
  );
}

export function ServicesSection({ locale = "tr" }: { locale?: "tr" | "en" }) {
  const isEnglish = locale === "en";
  const services = isEnglish ? servicesEn : servicesTr;
  const [openIndex, setOpenIndex] = useState<number | null>(null);

  return (
    <section
      className={styles.section}
      id="hizmetlerimiz"
      aria-labelledby="services-title"
    >
      <header className={styles.heading}>
        <div>
          <p className={styles.eyebrow}>
          {isEnglish ? "HOW WE SUPPORT YOU" : "NASIL DESTEK OLUYORUZ?"}
          </p>

          <h2 id="services-title">
            {isEnglish ? "Services that keep pace with your operation." : "İşletmenizin ritmine uyum sağlayan hizmet anlayışı."}
          </h2>
        </div>

        <p className={styles.intro}>
          {isEnglish ? "With a broad portfolio and a focus on the HoReCa sector, Ekiphan makes it easier to find the right equipment. We approach every project around its space, operations and needs." : <>Ekiphan, geniş ürün portföyü ve HoReCa sektörüne
          odaklanan yaklaşımıyla aradığınız ekipmana
          ulaşmanızı kolaylaştırır. Her projeyi kullanım alanı,
          operasyon ve ihtiyaçlarınız doğrultusunda ele alırız.</>}
        </p>
      </header>

      <div className={styles.grid}>
        {services.map((service, index) => {
          const isOpen = openIndex === index;

          return (
            <article
              className={`${styles.card}${
                isOpen ? ` ${styles.open}` : ""
              }`}
              key={service.title}
            >
              <div className={styles.visual}>
                <img
                  src={service.image}
                  alt=""
                />

                <span>
                  {String(index + 1).padStart(2, "0")}
                </span>
              </div>

              <div className={styles.copy}>
                <span className={styles.icon}>
                  <ServiceIcon />
                </span>

                <h3>{service.title}</h3>

                <p>{service.description}</p>

                <div
                  className={styles.detail}
                  aria-hidden={!isOpen}
                >
                  <p>{service.detail}</p>
                </div>

                <div className={styles.actions}>
                  <button
                    type="button"
                    aria-expanded={isOpen}
                    onClick={() =>
                      setOpenIndex(
                        isOpen ? null : index
                      )
                    }
                  >
                    {isOpen
                      ? (isEnglish ? "Close Details" : "Detayı Kapat")
                      : (isEnglish ? "Explore Service" : "Hizmeti İncele")}

                    <b aria-hidden="true">
                      {isOpen ? "−" : "→"}
                    </b>
                  </button>

                  <Link href={isEnglish ? "/en/contact#contact-form" : "/iletisim#contact-form"}>
                    {isEnglish ? "Request a Quote" : "Teklif Alın"}{" "}
                    <span aria-hidden="true">
                      →
                    </span>
                  </Link>
                </div>
              </div>
            </article>
          );
        })}
      </div>

      <div className={styles.cta}>
        <p>{isEnglish ? "LET'S DISCUSS YOUR PROJECT" : "PROJENİZİ KONUŞALIM"}</p>

        <strong>
          {isEnglish ? "Tell us what you need and let's build the right solution together." : "İhtiyacınızı anlatın, çözümü birlikte oluşturalım."}
        </strong>

        <Link href={isEnglish ? "/en/contact#contact-form" : "/iletisim#contact-form"}>
          {isEnglish ? "Get in Touch" : "İletişime Geçin"}
        </Link>
      </div>
    </section>
  );
}
