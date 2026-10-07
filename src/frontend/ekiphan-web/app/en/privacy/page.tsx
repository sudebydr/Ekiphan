import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../../../components/public-header";

export const metadata: Metadata = { title: "Privacy Notice | Ekiphan", description: "Information about the processing of personal data for Ekiphan quote requests.", alternates: { canonical: "/en/privacy", languages: { tr: "/kvkk", en: "/en/privacy" } } };

export default function EnglishPrivacyPage() {
  return <main data-public-page style={{ minHeight: "100vh", background: "#f7f4ef" }}><PublicHeader currentPath="/en/privacy" /><article style={{ width: "min(100% - 2.5rem, 54rem)", margin: "0 auto", padding: "8rem 0 5rem", color: "#181818" }}><p style={{ color: "#a91f3d", fontWeight: 800, letterSpacing: ".16em", fontSize: ".75rem" }}>TEMPORARY NOTICE</p><h1 style={{ fontSize: "clamp(2.4rem, 6vw, 4.5rem)", margin: "0 0 1.5rem" }}>Privacy Notice</h1><p style={{ maxWidth: "42rem", fontSize: "1.1rem", lineHeight: 1.7 }}>The privacy notice will be published here once provided and approved by the customer. Consent in the quote form is currently a temporary development environment consent used to process your request.</p><Link href="/en/quote-list" style={{ color: "#a91f3d", fontWeight: 800 }}>← Back to quote request</Link></article></main>;
}
