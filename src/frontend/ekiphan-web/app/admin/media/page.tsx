import type { Metadata } from "next";
import { MediaAdminClient } from "./media-admin-client";

export const metadata: Metadata = {
  title: "Medya Yönetimi",
  description: "Ekiphan medya kütüphanesi ve katalog medya atamaları"
};

export default function MediaAdminPage() {
  return <MediaAdminClient />;
}
