import type { Metadata } from "next";
import { AdminDashboardClient } from "./admin-dashboard-client";

export const metadata: Metadata = {
  title: "Yönetim Paneli | Ekiphan"
};

export default function AdminPage() {
  return <AdminDashboardClient />;
}

