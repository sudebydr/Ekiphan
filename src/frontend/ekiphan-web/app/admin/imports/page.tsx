import type { Metadata } from "next";
import { ImportAdminClient } from "./import-admin-client";

export const metadata: Metadata = {
  title: "Ürün importu | Ekiphan Admin",
  description: "Ekiphan ürün verisi doğrulama ve yayınlama yönetimi"
};

export default function ImportAdminPage() {
  return <ImportAdminClient />;
}
