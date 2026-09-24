import type { Metadata } from "next";
import { ProductRelationAdminClient } from "./product-relation-admin-client";

export const metadata: Metadata = {
  title: "Ürün İlişkileri Yönetimi",
  description: "Ekiphan manuel ürün ilişkileri yönetimi"
};

export default function ProductRelationsAdminPage() {
  return <ProductRelationAdminClient />;
}
