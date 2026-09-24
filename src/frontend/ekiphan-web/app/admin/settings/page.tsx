import type { Metadata } from "next";
import { SettingsAdminClient } from "./settings-admin-client";

export const metadata: Metadata = { title: "Site Ayarları | Ekiphan" };

export default function SettingsPage() {
  return <SettingsAdminClient />;
}
