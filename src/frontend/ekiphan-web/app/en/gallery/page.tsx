import type { Metadata } from "next";
import GalleryPage from "../../galeri/page";

export const metadata: Metadata = { title: "Gallery | Ekiphan", description: "Explore Ekiphan projects and professional kitchen solutions.", alternates: { canonical: "/en/gallery", languages: { tr: "/galeri", en: "/en/gallery" } } };

export default function EnglishGalleryPage() { return GalleryPage({ locale: "en" }); }
