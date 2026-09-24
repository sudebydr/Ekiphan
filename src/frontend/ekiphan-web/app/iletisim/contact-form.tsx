"use client";
import { FormEvent, useEffect, useState } from "react";
import styles from "./contact.module.css";

export function ContactForm() {
  const [status, setStatus] = useState<string | null>(null);
  const [sending, setSending] = useState(false);
  const [reasons, setReasons] = useState<Array<{ id: string; name: string; isComplaintReason: boolean }>>([]);
  const [categories, setCategories] = useState<Array<{ id: string; contactReasonId: string; name: string }>>([]);
  const [reasonId, setReasonId] = useState("");
  useEffect(() => {
    void fetch("/api/contact-taxonomy")
      .then(async (response) => {
        if (!response.ok) throw new Error();
        return response.json() as Promise<{ reasons: typeof reasons; complaintCategories: typeof categories }>;
      })
      .then((value) => { setReasons(value.reasons); setCategories(value.complaintCategories); })
      .catch(() => setStatus("İletişim nedenleri yüklenemedi."));
  }, []);
  const selectedReason = reasons.find((item) => item.id === reasonId);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const formElement = event.currentTarget; setSending(true); setStatus(null); const form = new FormData(formElement);
    const response = await fetch("/api/contact", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ ...Object.fromEntries(form.entries()), languageCode: "tr", kvkkConsent: form.get("kvkkConsent") === "on" }) });
    if (response.ok) { formElement.reset(); setReasonId(""); setStatus("Mesaj\u0131n\u0131z al\u0131nd\u0131. Te\u015fekk\u00fcr ederiz."); } else { const body = (await response.json()) as { detail?: string }; setStatus(body.detail ?? "Mesaj\u0131n\u0131z g\u00f6nderilemedi."); } setSending(false);
  }
  return <form className={styles.form} onSubmit={submit}>
    <div className={styles.row}><label className={styles.srOnly}>Ad soyad<input name="fullName" required maxLength={200} autoComplete="name" placeholder="Ad soyad" /></label><label className={styles.srOnly}>E-posta adresiniz<input name="email" required type="email" maxLength={320} autoComplete="email" placeholder="E-posta adresiniz" /></label></div>
    <div className={styles.row}><label className={styles.srOnly}>{"Telefon numaran\u0131z"}<input name="phone" maxLength={50} autoComplete="tel" placeholder={"Telefon numaran\u0131z"} /></label><label className={styles.srOnly}>{"\u015eirket / Firma"}<input name="companyName" maxLength={200} autoComplete="organization" placeholder={"\u015eirket / Firma"} /></label></div>
    <label className={styles.srOnly}>{"İletişim nedeni"}<select name="contactReasonId" required value={reasonId} onChange={(e) => setReasonId(e.target.value)}><option value="" disabled>Neden seçin</option>{reasons.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
    {selectedReason?.isComplaintReason && <label className={styles.srOnly}>{"Şikâyet kategorisi"}<select name="complaintCategoryId" required defaultValue=""><option value="" disabled>Kategori seçin</option>{categories.filter((item) => item.contactReasonId === reasonId).map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>}
    <label className={styles.srOnly}>{"Konu"}<input name="subject" required maxLength={200} placeholder="Konu" /></label>
    <label className={styles.srOnly}>{"Mesaj\u0131n\u0131z"}<textarea name="message" required minLength={10} maxLength={4000} placeholder={"Mesaj\u0131n\u0131z..."} /></label><label className={styles.honeypot} aria-hidden="true">{"Web sitesi"}<input name="website" tabIndex={-1} autoComplete="off" /></label><label className={styles.consent}><input name="kvkkConsent" type="checkbox" required />{"Ki\u015fisel verilerimin ileti\u015fim talebimin yan\u0131tlanmas\u0131 amac\u0131yla i\u015flenmesini kabul ediyorum."}</label><button type="submit" disabled={sending}>{sending ? "G\u00f6nderiliyor\u2026" : "Mesaj G\u00f6nder"}<span aria-hidden="true">{"\u2192"}</span></button>{status && <p className={styles.status} role="status">{status}</p>}
  </form>;
}
