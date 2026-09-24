import type { Metadata } from "next";
import { BrandAdminClient } from "./brand-admin-client";

export const metadata: Metadata = {
  title: "Marka Yönetimi",
  description: "Ekiphan marka ve iş ortağı temel kayıt yönetimi"
};

export default function BrandAdminPage() {
  return <BrandAdminClient />;
}
