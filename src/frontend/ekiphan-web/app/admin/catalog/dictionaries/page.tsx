import type { Metadata } from "next";
import { DictionaryAdminClient } from "./dictionary-admin-client";

export const metadata: Metadata = {
  title: "Etiket ve Birim Yönetimi",
  description: "Ekiphan etiket, ölçü birimi ve ürün etiketi yönetimi"
};

export default function DictionaryAdminPage() {
  return <DictionaryAdminClient />;
}
