import type { Metadata } from "next";
import { Suspense } from "react";
import { ContactAdminClient } from "./contact-admin-client";

export const metadata: Metadata = { title: "İletişim Talepleri | Ekiphan" };
export default function ContactAdminPage() {
  return <>
    <Suspense fallback={<p>İletişim talepleri yükleniyor…</p>}>
      <ContactAdminClient />
    </Suspense>
  </>;
}
