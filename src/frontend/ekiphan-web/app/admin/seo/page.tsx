import type { Metadata } from "next";
import { SeoAdminClient } from "./seo-admin-client";
export const metadata: Metadata = { title: "SEO Yönetimi | Ekiphan" };
export default function SeoPage() { return <SeoAdminClient />; }
