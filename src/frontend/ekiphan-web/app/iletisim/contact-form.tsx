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

export function ContactForm() {
  const [status, setStatus] = useState<string | null>(null);
  const [sending, setSending] = useState(false);
  const [reasons, setReasons] = useState<ContactReason[]>([]);
  const [categories, setCategories] = useState<ComplaintCategory[]>([]);
  const [reasonId, setReasonId] = useState("");

  useEffect(() => {
    void fetch("/api/contact-taxonomy")
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
        setStatus("İletişim nedenleri yüklenemedi.");
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
          languageCode: "tr",
          kvkkConsent: form.get("kvkkConsent") === "on",
        }),
      });

      if (response.ok) {
        formElement.reset();
        setReasonId("");
        setStatus("Mesajınız alındı. Teşekkür ederiz.");
      } else {
        const body = (await response.json()) as {
          detail?: string;
        };

        setStatus(
          body.detail ?? "Mesajınız gönderilemedi."
        );
      }
    } catch {
      setStatus("Mesajınız gönderilemedi.");
    } finally {
      setSending(false);
    }
  }

  return (
    <form className={styles.form} onSubmit={submit}>
      <div className={styles.row}>
        <label className={styles.srOnly}>
          Ad soyad
          <input
            name="fullName"
            required
            maxLength={200}
            autoComplete="name"
            placeholder="Ad soyad"
          />
        </label>

        <label className={styles.srOnly}>
          E-posta adresiniz
          <input
            name="email"
            required
            type="email"
            maxLength={320}
            autoComplete="email"
            placeholder="E-posta adresiniz"
          />
        </label>
      </div>

      <div className={styles.row}>
        <label className={styles.srOnly}>
          Telefon numaranız
          <input
            name="phone"
            maxLength={50}
            autoComplete="tel"
            placeholder="Telefon numaranız"
          />
        </label>

        <label className={styles.srOnly}>
          Şirket / Firma
          <input
            name="companyName"
            maxLength={200}
            autoComplete="organization"
            placeholder="Şirket / Firma"
          />
        </label>
      </div>

      <label className={styles.srOnly}>
        İletişim nedeni

        <select
          name="contactReasonId"
          required
          value={reasonId}
          onChange={(event) => setReasonId(event.target.value)}
        >
          <option value="" disabled>
            Neden seçin
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
          Şikâyet kategorisi

          <select
            name="complaintCategoryId"
            required
            defaultValue=""
          >
            <option value="" disabled>
              Kategori seçin
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
        Konu
        <input
          name="subject"
          required
          maxLength={200}
          placeholder="Konu"
        />
      </label>

      <label className={styles.srOnly}>
        Mesajınız
        <textarea
          name="message"
          required
          minLength={10}
          maxLength={4000}
          placeholder="Mesajınız..."
        />
      </label>

      <label
        className={styles.honeypot}
        aria-hidden="true"
      >
        Web sitesi
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
        Kişisel verilerimin iletişim talebimin yanıtlanması
        amacıyla işlenmesini kabul ediyorum.
      </label>

      <button type="submit" disabled={sending}>
        {sending ? "Gönderiliyor…" : "Mesaj Gönder"}
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