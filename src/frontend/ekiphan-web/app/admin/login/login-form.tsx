"use client";

import { FormEvent, useRef, useState } from "react";
import styles from "./login.module.css";

type Challenge = { token: string; methods: string[] };

function safeReturnUrl() {
  const requested = new URLSearchParams(window.location.search).get("returnUrl");
  return requested?.startsWith("/admin") && !requested.startsWith("//") && requested !== "/admin/login" ? requested : "/admin";
}

export function LoginForm() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [challenge, setChallenge] = useState<Challenge | null>(null);
  const errorRef = useRef<HTMLDivElement>(null);

  function showError(message: string) {
    setError(message);
    window.setTimeout(() => errorRef.current?.focus(), 0);
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (busy) return;
    const data = new FormData(event.currentTarget);
    const email = String(data.get("email") ?? "").trim();
    const password = String(data.get("password") ?? "");
    if (!email || !/^\S+@\S+\.\S+$/.test(email) || !password) {
      showError("Geçerli e-posta adresinizi ve parolanızı girin.");
      return;
    }
    setBusy(true); setError(null);
    try {
      const response = await fetch("/api/admin/auth/login", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ email, password }) });
      const body = await response.json().catch(() => ({})) as { requiresTwoFactor?: boolean; twoFactorToken?: string; allowedMethods?: string[]; detail?: string };
      if (!response.ok) {
        if (response.status === 423) throw new Error("Çok sayıda başarısız deneme nedeniyle hesabınız geçici olarak kilitlendi.");
        if (response.status === 429) throw new Error("Çok fazla giriş denemesi yapıldı. Lütfen daha sonra tekrar deneyin.");
        if (response.status === 502 || response.status === 503) throw new Error("Giriş servisine şu anda ulaşılamıyor. Lütfen kısa süre sonra tekrar deneyin.");
        throw new Error("E-posta veya parola hatalı.");
      }
      if (body.requiresTwoFactor && body.twoFactorToken) {
        setChallenge({ token: body.twoFactorToken, methods: body.allowedMethods?.length ? body.allowedMethods : ["Totp"] });
        return;
      }
      window.location.assign(safeReturnUrl());
    } catch (reason) { showError(reason instanceof Error ? reason.message : "Beklenmeyen bir hata oluştu."); }
    finally { setBusy(false); }
  }

  async function verify(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!challenge || busy) return;
    const data = new FormData(event.currentTarget);
    const code = String(data.get("code") ?? "").trim();
    if (!code) { showError("Doğrulama kodunu girin."); return; }
    setBusy(true); setError(null);
    try {
      const response = await fetch("/api/admin/auth/verify-two-factor", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ twoFactorToken: challenge.token, code, method: String(data.get("method") ?? challenge.methods[0]) }) });
      if (!response.ok) throw new Error("Doğrulama kodu geçersiz veya süresi dolmuş.");
      window.location.assign(safeReturnUrl());
    } catch (reason) { showError(reason instanceof Error ? reason.message : "Doğrulama tamamlanamadı."); }
    finally { setBusy(false); }
  }

  return <main className={styles.pageWrapper}><div className={styles.card}>
    <div className={styles.banner}><div className={styles.bannerLogoContainer}><p className={styles.bannerLogo}>EKİPHAN<span>.</span></p><small className={styles.bannerSubtitle}>PROFESYONEL MUTFAK ÇÖZÜMLERİ</small></div></div>
    <p>EKİPHAN · YÖNETİM</p><h1>{challenge ? "Doğrulama Kodu" : "Yönetim Paneli"}</h1>
    <span>{challenge ? "Girişinizi tamamlamak için doğrulama kodunuzu girin." : "İçeriklerinizi ve ürün kataloğunuzu güvenle yönetin."}</span>
    {error && <div ref={errorRef} tabIndex={-1} className={styles.errorAlert} role="alert">{error}</div>}
    {challenge ? <form onSubmit={verify} noValidate><div className={styles.field}><label htmlFor="codeInput">Doğrulama kodu</label><div className={styles.inputWrapper}><input id="codeInput" name="code" inputMode="numeric" autoComplete="one-time-code" maxLength={32} required aria-invalid={!!error} autoFocus /></div></div><input name="method" type="hidden" value={challenge.methods[0]} /><button type="submit" className={styles.submitBtn} disabled={busy}>{busy ? "Doğrulanıyor..." : "Girişi tamamla"}</button><button className={styles.backLink} type="button" onClick={() => { setChallenge(null); setError(null); }}>Girişe geri dön</button></form> : <form onSubmit={submit} noValidate><div className={styles.field}><label htmlFor="emailInput">E-posta</label><div className={styles.inputWrapper}><input id="emailInput" name="email" type="email" autoComplete="username" maxLength={254} required aria-invalid={!!error} /></div></div><div className={styles.field}><label htmlFor="passwordInput">Parola</label><div className={styles.inputWrapper}><input id="passwordInput" name="password" type={showPassword ? "text" : "password"} autoComplete="current-password" maxLength={256} required aria-invalid={!!error} /><button type="button" className={styles.toggleButton} onClick={() => setShowPassword((value) => !value)} aria-label={showPassword ? "Parolayı gizle" : "Parolayı göster"}>{showPassword ? (
  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d="M3 3l18 18"/><path d="M10.6 10.6a2 2 0 0 0 2.8 2.8"/><path d="M9.9 4.2A10.7 10.7 0 0 1 12 4c7 0 11 8 11 8a18.8 18.8 0 0 1-3 4.2M6.1 6.1A18.3 18.3 0 0 0 1 12s4 8 11 8a10.8 10.8 0 0 0 4.2-.9"/></svg>
) : (
  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
)}</button></div></div><button type="submit" className={styles.submitBtn} disabled={busy}>{busy ? "Giriş yapılıyor..." : "Giriş yap"}</button></form>}
    <a href="/" className={styles.backLink}>← Ana siteye dön</a><div className={styles.securityNote}>Güvenli yönetim erişimi</div>
  </div></main>;
}