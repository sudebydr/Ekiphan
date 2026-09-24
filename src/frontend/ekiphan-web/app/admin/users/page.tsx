import type { Metadata } from "next";
import { UserAdminClient } from "./user-admin-client";

export const metadata: Metadata = {
  title: "Kullanıcılar ve İzinler | Ekiphan"
};

export default function AdminUsersPage() {
  return <UserAdminClient />;
}

