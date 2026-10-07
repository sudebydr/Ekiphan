import type { Metadata } from "next";
import ShowroomPage from "../../showroom/page";

export const metadata: Metadata = { title: "Showroom | Ekiphan", description: "Visit the Ekiphan showroom and explore our professional kitchen solutions.", alternates: { canonical: "/en/showroom", languages: { tr: "/showroom", en: "/en/showroom" } } };

export default function EnglishShowroomPage() { return ShowroomPage({ locale: "en" }); }
