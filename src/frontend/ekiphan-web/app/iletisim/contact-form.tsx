"use client";

import { FormEvent, useEffect, useState } from "react";
import styles from "./contact.module.css";

type ContactReason = {
  id: string;
  name: string;
  isComplaintReason: boolean;
};

type ComplaintCategory = {
  id: string;
  contactReasonId: string;
  name: string;
};

type ContactTaxonomy = {
  reasons: ContactReason[];
  complaintCategories: ComplaintCategory[];
};

export function ContactForm({ locale = "tr" }: { locale?: "tr" | "en" }) {
  const en = locale === "en";
  const [status, setStatus] = useState<string | null>(null);
  const [sending, setSending] = useState(false);
  const [reasons, setReasons] = useState<ContactReason[]>([]);
  const [categories, setCategories] = useState<ComplaintCategory[]>([]);
  const [reasonId, setReasonId] = useState("");

  useEffect(() => {
    void fetch("/api/contact-taxonomy", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error();
        }

        return response.json() as Promise<ContactTaxonomy>;
      })
      .then((value) => {
        setReasons(value.reasons);
        setCategories(value.complaintCategories);
      })
      .catch(() => {
          setStatus(en ? "Contact options could not be loaded." : "İletişim nedenleri yüklenemedi.");
      });
  }, []);

  const selectedReason = reasons.find((item) => item.id === reasonId);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const formElement = event.currentTarget;

    setSending(true);
    setStatus(null);

    const form = new FormData(formElement);

    try {
      const response = await fetch("/api/contact", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          ...Object.fromEntries(form.entries()),
          contactReasonId: reasonId,
          complaintCategoryId: selectedReason?.isComplaintReason
            ? form.get("complaintCategoryId") : null,
          languageCode: locale,
          kvkkConsent: form.get("kvkkConsent") === "on",
        }),
      });

      if (response.ok) {
        formElement.reset();
        setReasonId("");
        setStatus(en ? "Your message has been received. Thank you." : "Mesajınız alındı. Teşekkür ederiz.");
      } else {
        const body = (await response.json()) as {
          detail?: string;
        };

        setStatus(
          body.detail ?? (en ? "Your message could not be sent." : "Mesajınız gönderilemedi.")
        );
      }
    } catch {
      setStatus(en ? "Your message could not be sent." : "Mesajınız gönderilemedi.");
    } finally {
      setSending(false);
    }
  }

  return (
    <form className={styles.form} onSubmit={submit}>
      <div className={styles.row}>
        <label className={styles.srOnly}>
          {en ? "Full Name" : "Ad soyad"}
          <input
            name="fullName"
            required
            maxLength={200}
            autoComplete="name"
            placeholder={en ? "Full name" : "Ad soyad"}
          />
        </label>

        <label className={styles.srOnly}>
          {en ? "Email Address" : "E-posta adresiniz"}
          <input
            name="email"
            required
            type="email"
            maxLength={320}
            autoComplete="email"
            placeholder={en ? "Email address" : "E-posta adresiniz"}
          />
        </label>
      </div>

      <div className={styles.row}>
        <label className={styles.srOnly}>
          {en ? "Phone Number" : "Telefon numaranız"}
          <input
            name="phone"
            maxLength={50}
            autoComplete="tel"
            placeholder={en ? "Phone number" : "Telefon numaranız"}
          />
        </label>

        <label className={styles.srOnly}>
          {en ? "Company Name" : "Şirket / Firma"}
          <input
            name="companyName"
            maxLength={200}
            autoComplete="organization"
            placeholder={en ? "Company name" : "Şirket / Firma"}
          />
        </label>
      </div>

      <label className={styles.srOnly}>
        {en ? "Reason for Contact" : "İletişim nedeni"}

        <select
          name="contactReasonId"
          required
          value={reasonId}
          onChange={(event) => setReasonId(event.target.value)}
        >
          <option value="" disabled>
            {en ? "Select a reason" : "Neden seçin"}
          </option>

          {reasons.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </select>
      </label>

      {selectedReason?.isComplaintReason && (
        <label className={styles.srOnly}>
          {en ? "Complaint Category" : "Şikâyet kategorisi"}

          <select
            key={reasonId}
            name="complaintCategoryId"
            required
            defaultValue=""
          >
            <option value="" disabled>
              {en ? "Select a category" : "Kategori seçin"}
            </option>

            {categories
              .filter((item) => item.contactReasonId === reasonId)
              .map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
          </select>
        </label>
      )}

      <label className={styles.srOnly}>
        {en ? "Subject" : "Konu"}
        <input
          name="subject"
          required
          maxLength={200}
          placeholder={en ? "Subject" : "Konu"}
        />
      </label>

      <label className={styles.srOnly}>
        {en ? "Your Message" : "Mesajınız"}
        <textarea
          name="message"
          required
          minLength={10}
          maxLength={4000}
          placeholder={en ? "Your message..." : "Mesajınız..."}
        />
      </label>

      <label
        className={styles.honeypot}
        aria-hidden="true"
      >
        {en ? "Website" : "Web sitesi"}
        <input
          name="website"
          tabIndex={-1}
          autoComplete="off"
        />
      </label>

      <label className={styles.consent}>
        <input
          name="kvkkConsent"
          type="checkbox"
          required
        />
        {en ? "I consent to the processing of my personal data to respond to my enquiry." : "Kişisel verilerimin iletişim talebimin yanıtlanması amacıyla işlenmesini kabul ediyorum."}
      </label>

      <button type="submit" disabled={sending}>
        {sending ? (en ? "Sending…" : "Gönderiliyor…") : (en ? "Send Message" : "Mesaj Gönder")}
        <span aria-hidden="true">→</span>
      </button>

      {status && (
        <p className={styles.status} role="status">
          {status}
        </p>
      )}
    </form>
  );
}
