import type { Metadata } from "next";
import { VariantAdminClient } from "./variant-admin-client";

export const metadata: Metadata = {
  title: "Varyant Yönetimi",
  description: "Ekiphan ürün varyant grubu, seçeneği ve SKU yönetimi"
};

export default function VariantAdminPage() {
  return <VariantAdminClient />;
}
