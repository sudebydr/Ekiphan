import type { Metadata } from "next";
import { LoginForm } from "./login-form";

export const metadata: Metadata = {
  title: "Admin Girişi"
};

export default function AdminLoginPage() {
  return <LoginForm />;
}
