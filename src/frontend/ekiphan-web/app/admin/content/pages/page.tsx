import type { Metadata } from "next";
import { ContentAdminClient } from "./content-admin-client";

export const metadata: Metadata = {
  title: "Kurumsal İçerikler | Ekiphan"
};

export default function ContentAdminPage() {
  return <ContentAdminClient />;
}

