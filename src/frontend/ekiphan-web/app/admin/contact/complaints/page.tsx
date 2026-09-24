import type { Metadata } from "next";
import { ComplaintAdminClient } from "./complaint-admin-client";

export const metadata: Metadata = { title: "Müşteri Şikâyetleri | Ekiphan" };

export default function ComplaintsPage() {
  return <ComplaintAdminClient />;
}
