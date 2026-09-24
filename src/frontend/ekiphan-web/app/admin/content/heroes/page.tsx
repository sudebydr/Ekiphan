import type { Metadata } from "next";
import { HomepageHeroAdminClient } from "./homepage-hero-admin-client";

export const metadata: Metadata = { title: "Ana Sayfa Hero Yönetimi | Ekiphan" };

export default function HomepageHeroesPage() {
  return <HomepageHeroAdminClient />;
}
