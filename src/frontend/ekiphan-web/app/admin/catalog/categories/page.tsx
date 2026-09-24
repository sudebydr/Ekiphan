import type { Metadata } from "next";
import { CategoryAdminClient } from "./category-admin-client";

export const metadata: Metadata = {
  title: "Kategori Yönetimi",
  description: "Ekiphan ürün bölümü ve kategori hiyerarşisi yönetimi"
};

export default function CategoryAdminPage() {
  return <CategoryAdminClient />;
}
