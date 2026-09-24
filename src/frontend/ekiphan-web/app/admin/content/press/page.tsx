import type { Metadata } from "next";
import { PressReleaseAdminClient } from "./press-release-admin-client";

export const metadata: Metadata = { title: "Basın Odası Yönetimi | Ekiphan" };

export default function PressReleaseAdminPage() {
  return <PressReleaseAdminClient />;
}
