"use client";

import Link from "next/link";
import { useState } from "react";
import styles from "./services-section.module.css";

const services = [
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

export function ServicesSection() {
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
            NASIL DESTEK OLUYORUZ?
          </p>

          <h2 id="services-title">
            İşletmenizin ritmine uyum sağlayan hizmet anlayışı.
          </h2>
        </div>

        <p className={styles.intro}>
          Ekiphan, geniş ürün portföyü ve HoReCa sektörüne
          odaklanan yaklaşımıyla aradığınız ekipmana
          ulaşmanızı kolaylaştırır. Her projeyi kullanım alanı,
          operasyon ve ihtiyaçlarınız doğrultusunda ele alırız.
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
                      ? "Detayı Kapat"
                      : "Hizmeti İncele"}

                    <b aria-hidden="true">
                      {isOpen ? "−" : "→"}
                    </b>
                  </button>

                  <Link href="/iletisim#contact-form">
                    Teklif Alın{" "}
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
        <p>PROJENİZİ KONUŞALIM</p>

        <strong>
          İhtiyacınızı anlatın, çözümü birlikte oluşturalım.
        </strong>

        <Link href="/iletisim#contact-form">
          İletişime Geçin
        </Link>
      </div>
    </section>
  );
}