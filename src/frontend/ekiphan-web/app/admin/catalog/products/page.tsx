import type { Metadata } from "next";
import { ProductAdminClient } from "./product-admin-client";

export const metadata: Metadata = {
  title: "Ürün Yönetimi",
  description: "Ekiphan temel ürün ve çeviri yönetimi"
};

export default function ProductAdminPage() {
  return <ProductAdminClient />;
}
