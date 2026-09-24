import type { Metadata } from "next";
import type { ReactNode } from "react";
import { AdminSessionGuard } from "./admin-session-guard";

export const metadata: Metadata = {
  robots: {
    index: false,
    follow: false,
    nocache: true
  }
};

export default function AdminLayout({
  children
}: Readonly<{ children: ReactNode }>) {
  return (
    <div className="admin-scope">
      <AdminSessionGuard>{children}</AdminSessionGuard>
    </div>
  );
}
