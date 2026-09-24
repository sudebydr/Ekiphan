import type { Metadata } from "next";
import { Suspense } from "react";
import { QuoteAdminClient } from "./quote-admin-client";

export const metadata: Metadata = {
  title: "Teklif Yönetimi",
  description: "Ekiphan teklif talepleri ve durum yönetimi"
};

export default function QuoteAdminPage() {
  return <Suspense fallback={<p>Teklif talepleri yükleniyor…</p>}>
    <QuoteAdminClient />
  </Suspense>;
}
