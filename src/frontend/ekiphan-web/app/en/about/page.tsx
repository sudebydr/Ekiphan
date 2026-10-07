import type { Metadata } from "next";
import AboutPage from "../../hakkimizda/page";

export const metadata: Metadata = { title: "About Us | Ekiphan", description: "Ekiphan's approach, values and working principles for professional kitchen solutions.", alternates: { canonical: "/en/about", languages: { tr: "/hakkimizda", en: "/en/about" } } };

export default function EnglishAboutPage() {
  return AboutPage({ locale: "en" });
}
