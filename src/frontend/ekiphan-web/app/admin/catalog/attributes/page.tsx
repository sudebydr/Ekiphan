import type { Metadata } from "next";
import { AttributeAdminClient } from "./attribute-admin-client";

export const metadata: Metadata = {
  title: "Ürün Özellikleri",
  description: "Ekiphan dinamik ürün özelliği ve kategori kuralı yönetimi"
};

export default function AttributeAdminPage() {
  return <AttributeAdminClient />;
}
