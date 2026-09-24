import type { Metadata } from "next";
import { MenuAdminClient } from "./menu-admin-client";

export const metadata: Metadata = {
  title: "Menü Yönetimi | Ekiphan"
};

export default function MenuAdminPage() {
  return <MenuAdminClient />;
}

