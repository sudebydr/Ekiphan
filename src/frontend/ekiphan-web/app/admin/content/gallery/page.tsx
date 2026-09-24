import type { Metadata } from "next";
import { GalleryAdminClient } from "./gallery-admin-client";

export const metadata: Metadata = { title: "Galeri Yönetimi | Ekiphan" };

export default function GalleryAdminPage() {
  return <GalleryAdminClient />;
}
