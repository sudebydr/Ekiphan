"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import {
  LocalQuoteItem,
  readQuoteList,
  writeQuoteList
} from "../../lib/quote-list";
import { getDemoProduct } from "../../lib/demo-products";
import styles from "./quote.module.css";

type Props = {
  kvkkNoticeUrl: string | null;
  locale?: "tr" | "en";
};

type ProblemResponse = {
  detail?: string;
};
function itemImageUrl(item: LocalQuoteItem): string | null {
  if (item.imageUrl) return item.imageUrl;
  if (!item.productId.startsWith("demo-")) return null;
  return getDemoProduct(item.slug)?.images[0]?.url ?? null;
}

function itemInitials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toLocaleUpperCase("tr-TR");
}

export function QuoteRequestClient({ kvkkNoticeUrl, locale = "tr" }: Props) {
  const en = locale === "en";
  const [items, setItems] = useState<LocalQuoteItem[]>([]);
  const [ready, setReady] = useState(false);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");
  const [requestNumber, setRequestNumber] = useState("");

  useEffect(() => {
    setItems(readQuoteList());
    setReady(true);
  }, []);

  function updateItem(
    productId: string,
    patch: Partial<Pick<LocalQuoteItem, "quantity" | "note">>
  ) {
    const next = items.map((item) =>
      item.productId === productId ? { ...item, ...patch } : item
    );
    if (!writeQuoteList(next)) {
      setError(en ? "Your browser does not allow saving the quote list." : "Tarayıcınız teklif listesini kaydetmeye izin vermiyor.");
      return;
    }
    setItems(next);
  }

  function removeItem(productId: string) {
    const next = items.filter((item) => item.productId !== productId);
    if (!writeQuoteList(next)) {
      setError(en ? "Your browser does not allow saving the quote list." : "Tarayıcınız teklif listesini kaydetmeye izin vermiyor.");
      return;
    }
    setItems(next);
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!kvkkNoticeUrl || items.length === 0 || sending) return;
    const formElement = event.currentTarget;
    setSending(true);
    setError("");
    setRequestNumber("");

    const form = new FormData(event.currentTarget);
    const payload = {
      fullName: String(form.get("fullName") ?? ""),
      companyName: String(form.get("companyName") ?? ""),
      phone: String(form.get("phone") ?? ""),
      email: String(form.get("email") ?? ""),
      country: String(form.get("country") ?? ""),
      city: String(form.get("city") ?? ""),
      sector: String(form.get("sector") ?? ""),
      projectName: String(form.get("projectName") ?? ""),
      message: String(form.get("message") ?? ""),
      languageCode: locale,
      kvkkConsent: form.get("kvkkConsent") === "on",
      commercialCommunicationConsent:
        form.get("commercialConsent") === "on",
      website: String(form.get("website") ?? ""),
      items: items.map((item) => {
        const isDemoProduct = item.productId.startsWith("demo-");
        return {
          productId: isDemoProduct ? null : item.productId,
          variantId: null,
          quantity: item.quantity,
          note: item.note || null,
          productName: isDemoProduct ? item.name : null,
          sku: isDemoProduct ? item.sku : null,
          brand: isDemoProduct ? item.brandName : null,
          imageUrl: null
        };
      })
    };

    try {
      const response = await fetch("/api/quotes", {
        method: "POST",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json"
        },
        body: JSON.stringify(payload)
      });
      const body = (await response.json().catch(() => ({}))) as
        ProblemResponse & { requestNumber?: string };
      if (!response.ok || !body.requestNumber) {
        setError(
          body.detail ??
            (en ? "Your quote request could not be sent. Please try again later." : "Teklif talebiniz gönderilemedi. Lütfen daha sonra tekrar deneyin.")
        );
        return;
      }

      setRequestNumber(body.requestNumber);
      setItems([]);
      writeQuoteList([]);
      formElement.reset();
    } catch {
      setError(
        (en ? "The quote service is unavailable. Please try again later." : "Teklif servisine ulaşılamıyor. Lütfen daha sonra tekrar deneyin.")
      );
    } finally {
      setSending(false);
    }
  }

  return (
    <div className={styles.layout}>
      <section className={styles.panel} aria-labelledby="quote-items-title">
        <div className={styles.panelHeading}>
          <h2 id="quote-items-title">{en ? "Selected Products" : "Seçtiğiniz ürünler"}</h2>
          <span className={styles.itemCount}>
            {ready ? items.length : "—"} {en ? "items" : "ürün"}
          </span>
        </div>
        {!ready ? (
          <p aria-live="polite">{en ? "Preparing your quote list…" : "Teklif listeniz hazırlanıyor…"}</p>
        ) : items.length === 0 ? (
          <div className={styles.empty}>
            <svg className={styles.emptyIcon} viewBox="0 0 48 48" aria-hidden="true">
              <path d="m8 16 16-9 16 9v17L24 42 8 33V16Z" />
              <path d="m8 16 16 9 16-9M24 25v17M16 11l16 9" />
            </svg>
            <p className={styles.emptyTitle}>{en ? "Your quote list is empty." : "Teklif listenizde henüz ürün bulunmuyor."}</p>
            <p className={styles.emptyDescription}>
              {en ? "Browse the products and add items to request a quote." : "Teklif alabilmek için ürünleri inceleyip listenize ekleyin."}
            </p>
            <Link className={styles.secondaryButton} href={en ? "/en/products" : "/katalog"}>
              {en ? "View Products" : "Ürünlere git"} <span aria-hidden="true">→</span>
            </Link>
          </div>
        ) : (
          <div className={styles.itemList}>
            {items.map((item) => (
              <article className={styles.item} key={item.productId}>
                <Link
                  className={styles.itemVisual}
                  href={`/katalog/${encodeURIComponent(item.slug)}`}
                  aria-label={en ? `View ${item.name}` : `${item.name} ürününü görüntüle`}
                >
                  {itemImageUrl(item) ? (
                    <img src={itemImageUrl(item) ?? ""} alt={item.name} loading="lazy" />
                  ) : (
                    <span aria-hidden="true">{itemInitials(item.name)}</span>
                  )}
                </Link>
                <div className={styles.itemHeader}>
                  <div>
                    <h3>
                      <Link
                        className={styles.itemName}
                        href={`/katalog/${encodeURIComponent(item.slug)}`}
                      >
                        {item.name}
                      </Link>
                    </h3>
                    <p className={styles.itemMeta}>
                      {item.brandName ? `${item.brandName} · ` : ""}
                      {en ? "Code:" : "Kod:"} {item.sku}
                    </p>
                  </div>
                </div>
                <div className={styles.itemFields}>
                  <div className={styles.field}>
                    <label htmlFor={`quantity-${item.productId}`}>{en ? "Quantity" : "Adet"}</label>
                    <div className={styles.quantityStepper}>
                      <button
                        type="button"
                        onClick={() => updateItem(item.productId, { quantity: Math.max(1, item.quantity - 1) })}
                        aria-label={en ? "Decrease quantity" : "Adedi azalt"}
                        disabled={item.quantity <= 1}
                      >
                        −
                      </button>
                      <input
                        id={`quantity-${item.productId}`}
                        type="number"
                        min={1}
                        max={100000}
                        value={item.quantity}
                        onChange={(event) => {
                          const value = Number(event.target.value);
                          if (Number.isInteger(value) && value >= 1) {
                            updateItem(item.productId, {
                              quantity: Math.min(value, 100_000)
                            });
                          }
                        }}
                      />
                      <button
                        type="button"
                        onClick={() => updateItem(item.productId, { quantity: Math.min(100_000, item.quantity + 1) })}
                        aria-label={en ? "Increase quantity" : "Adedi artır"}
                        disabled={item.quantity >= 100_000}
                      >
                        +
                      </button>
                    </div>
                  </div>
                  <div className={styles.field}>
                    <label htmlFor={`note-${item.productId}`}>
                      {en ? "Product note" : "Ürün notu"}
                    </label>
                    <textarea
                      id={`note-${item.productId}`}
                      maxLength={2000}
                      value={item.note}
                      onChange={(event) =>
                        updateItem(item.productId, {
                          note: event.target.value
                        })
                      }
                      placeholder={en ? "Colour, use case or project note" : "Renk, kullanım alanı veya proje notu"}
                    />
                  </div>
                </div>
                <div className={styles.itemActions}>
                  <button
                    className={styles.textButton}
                    type="button"
                    onClick={() => removeItem(item.productId)}
                    aria-label={en ? `Remove ${item.name} from quote list` : `${item.name} ürününü teklif listesinden kaldır`}
                  >
                    {en ? "Remove from list" : "Listeden kaldır"}
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      <section className={`${styles.panel} ${styles.contactPanel}`} aria-labelledby="contact-title">
        <div className={styles.panelHeading}>
          <h2 id="contact-title">{en ? "Your Contact Details" : "İletişim bilgileriniz"}</h2>
        </div>
        {!kvkkNoticeUrl && (
          <div className={styles.legalNotice} role="status">
            {en ? "Quote submission is temporarily unavailable because the approved privacy notice has not been configured. Your product list will remain saved on this device." : <>Hukuk onaylı KVKK aydınlatma metni henüz sisteme bağlanmadığı için
            teklif gönderimi geçici olarak kapalıdır. Ürün listeniz bu cihazda
            korunur.</>}
          </div>
        )}
        {error && (
          <div className={styles.error} role="alert">
            {error}
          </div>
        )}
        {requestNumber && (
          <div className={styles.success} role="status">
            {en ? "Your request has been received. Reference number:" : "Talebiniz alındı. Takip numaranız:"} <strong>{requestNumber}</strong>
          </div>
        )}
        <form onSubmit={submit}>
          <div className={styles.formGrid}>
            <div className={styles.field}>
              <label htmlFor="fullName">{en ? "Full Name" : "Ad soyad"}</label>
              <input
                id="fullName"
                name="fullName"
                autoComplete="name"
                placeholder={en ? "Enter your full name" : "Adınızı ve soyadınızı girin"}
                maxLength={200}
                required
              />
            </div>
            <div className={styles.field}>
              <label htmlFor="companyName">{en ? "Company Name" : "Firma adı"}</label>
              <input
                id="companyName"
                name="companyName"
                autoComplete="organization"
                placeholder={en ? "Enter your company name" : "Firma adınızı girin"}
                maxLength={200}
                required
              />
            </div>
            <div className={styles.field}>
              <label htmlFor="phone">{en ? "Phone" : "Telefon"}</label>
              <input
                id="phone"
                name="phone"
                type="tel"
                autoComplete="tel"
                placeholder={en ? "Your phone number" : "Telefon numaranız"}
                maxLength={50}
                required
              />
            </div>
            <div className={styles.field}>
              <label htmlFor="email">{en ? "Email" : "E-posta"}</label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                placeholder="ornek@firma.com"
                maxLength={254}
                required
              />
            </div>
            <div className={styles.field}>
              <label htmlFor="country">{en ? "Country" : "Ülke"}</label>
              <input
                id="country"
                name="country"
                autoComplete="country-name"
                placeholder={en ? "Country" : "Ülke"}
                maxLength={100}
                defaultValue={en ? "Turkey" : "Türkiye"}
                required
              />
            </div>
            <div className={styles.field}>
              <label htmlFor="city">{en ? "City" : "Şehir"}</label>
              <input
                id="city"
                name="city"
                autoComplete="address-level2"
                placeholder={en ? "City" : "Şehir"}
                maxLength={100}
              />
            </div>
            <div className={styles.field}>
              <label htmlFor="sector">{en ? "Industry" : "Sektör"}</label>
              <input id="sector" name="sector" placeholder={en ? "Industry" : "Sektör"} maxLength={150} />
            </div>
            <div className={styles.field}>
              <label htmlFor="projectName">{en ? "Project Name" : "Proje adı"}</label>
              <input id="projectName" name="projectName" placeholder={en ? "Project Name" : "Proje adı"} maxLength={200} />
            </div>
            <div className={`${styles.field} ${styles.wide}`}>
              <label htmlFor="message">{en ? "Additional Notes" : "Genel notunuz"}</label>
              <textarea id="message" name="message" placeholder={en ? "Share any details about your project…" : "Projeniz hakkında detayları paylaşabilirsiniz…"} maxLength={4000} />
            </div>
            <div className={`${styles.checkbox} ${styles.wide}`}>
              <input
                id="kvkkConsent"
                name="kvkkConsent"
                type="checkbox"
                required
                disabled={!kvkkNoticeUrl}
              />
              <label htmlFor="kvkkConsent">
                {kvkkNoticeUrl ? (
                  <>
                    <a
                      href={kvkkNoticeUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      {en ? "privacy notice" : "KVKK aydınlatma metnini"}
                    </a>{" "}
                    {en ? " read and consent to the processing of my personal data for this request." : " okudum ve kişisel verilerimin talebim için işlenmesini kabul ediyorum."}
                  </>
                ) : (
                  (en ? "Privacy notice is not configured." : "KVKK aydınlatma metni yapılandırılmamış.")
                )}
              </label>
            </div>
            <div className={`${styles.checkbox} ${styles.wide}`}>
              <input
                id="commercialConsent"
                name="commercialConsent"
                type="checkbox"
              />
              <label htmlFor="commercialConsent">
                {en ? "I would like to receive commercial communications about offers and products. This is optional and independent of my quote request." : "Kampanya ve ürün duyuruları için ticari ileti almak istiyorum. Bu seçim teklif talebinden bağımsız ve isteğe bağlıdır."}
              </label>
            </div>
            <div className={styles.honeypot} aria-hidden="true">
              <label htmlFor="website">{en ? "Website" : "Web sitesi"}</label>
              <input
                id="website"
                name="website"
                type="text"
                tabIndex={-1}
                autoComplete="off"
              />
            </div>
          </div>
          <div className={styles.formActions}>
            <button
              className={styles.primaryButton}
              type="submit"
              disabled={
                !kvkkNoticeUrl ||
                items.length === 0 ||
                sending ||
                Boolean(requestNumber)
              }
            >
              {sending ? (en ? "Sending…" : "Gönderiliyor…") : <>{en ? "Submit Quote Request" : "Teklif talebini gönder"} <span aria-hidden="true">→</span></>}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
