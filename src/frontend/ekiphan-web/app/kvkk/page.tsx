import type { Metadata } from "next";
import Link from "next/link";
import { PublicHeader } from "../../components/public-header";

export const metadata: Metadata = { title: "KVKK Aydınlatma Metni" };

export default function KvkkPage() {
  return <main data-public-page style={{ minHeight: "100vh", background: "#f7f4ef" }}>
    <PublicHeader currentPath="/kvkk" />
    <article style={{ width: "min(100% - 2.5rem, 54rem)", margin: "0 auto", padding: "8rem 0 5rem", color: "#181818" }}>
      <p style={{ color: "#a91f3d", fontWeight: 800, letterSpacing: ".16em", fontSize: ".75rem" }}>GEÇİCİ BİLGİLENDİRME</p>
      <h1 style={{ fontSize: "clamp(2.4rem, 6vw, 4.5rem)", margin: "0 0 1.5rem" }}>KVKK Aydınlatma Metni</h1>
      {/* TEMP: Replace this notice with the customer-approved KVKK text before production launch. */}
      <p style={{ maxWidth: "42rem", fontSize: "1.1rem", lineHeight: 1.7 }}>KVKK Aydınlatma Metni müşteri tarafından sağlandığında bu alanda yayınlanacaktır. Teklif formundaki onay, talebinizin işlenmesi amacıyla alınan geçici geliştirme ortamı onayıdır.</p>
      <Link href="/teklif-listem" style={{ color: "#a91f3d", fontWeight: 800 }}>← Teklif formuna dön</Link>
    </article>
  </main>;
}