import type { Metadata } from "next";
import { PublicHeader } from "../../components/public-header";
import { getContentPage, getPressReleases } from "../../lib/catalog-api";
import type { PublicPressRelease } from "../../lib/press-release-types";
import { managedMetadata } from "../../lib/managed-metadata";
import { PressShowcase } from "./press-showcase";
import styles from "./press.module.css";

export const dynamic = "force-dynamic";
type PressViewItem = { id: string; title: string; summary: string; date: string; image: string; alt: string; category: string; href?: string | null };
const demoNews: PressViewItem[] = [
  { id: "demo-1", category: "Kurumsal", date: "2026-08-12", title: "Ekiphan showroom deneyimi yeni akışıyla ziyaretçilerini bekliyor.", summary: "Profesyonel mutfak, servis ve sunum alanlarını bir araya getiren yeni showroom akışımız kullanıma açıldı.", image: "/images/gallery/showroom-2026-08-11/TEK_9546%20copy.jpg", alt: "Ekiphan showroomunda profesyonel mutfak uygulaması" },
  { id: "demo-2", category: "Projeler", date: "2026-07-28", title: "Proje ekipleri için planlama buluşmaları devam ediyor.", summary: "Mekân ihtiyaçlarını doğru okumaya odaklanan teknik planlama buluşmalarımız sürüyor.", image: "/images/gallery/showroom-2026-08-11/TEK_9628%20copy.jpg", alt: "Profesyonel mutfak proje detayları" },
  { id: "demo-3", category: "Ürünler", date: "2026-07-06", title: "Servis alanlarında sade ve işlevsel çözümler.", summary: "Dayanıklılık, kullanım konforu ve operasyon hızını bir araya getiren seçkilerimizi paylaşıyoruz.", image: "/images/gallery/showroom-2026-08-11/TEK_9560%20copy.jpg", alt: "Ekiphan servis ekipmanları" },
  { id: "demo-4", category: "Markalar", date: "2026-06-20", title: "Global markalarla güçlü iş ortaklıkları.", summary: "Profesyonel mutfak projeleri için seçilmiş markalarla ürün portföyümüz gelişiyor.", image: "/images/gallery/showroom-2026-08-11/TEK_9664%20copy.jpg", alt: "Profesyonel mutfak ekipmanı" },
  { id: "demo-5", category: "Fuarlar", date: "2026-05-14", title: "Yeni sezon profesyonel mutfak çözümleri bir arada.", summary: "Sektör paydaşlarıyla bir araya geldiğimiz buluşmadan öne çıkan notlar.", image: "/images/gallery/showroom-2026-08-11/TEK_9549%20copy.jpg", alt: "Ekiphan ürün sunumu" },
  { id: "demo-6", category: "Etkinlikler", date: "2026-04-03", title: "Şefler ve proje ekipleri showroomda buluştu.", summary: "Uygulama odaklı deneyim günümüzden kısa notlar ve yeni iş birlikleri.", image: "/images/gallery/showroom-2026-08-11/TEK_9562%20copy.jpg", alt: "Showroom etkinliğinden görünüm" },
  { id: "demo-7", category: "Sektör", date: "2025-12-19", title: "Profesyonel mutfaklarda verimli planlama yaklaşımı.", summary: "Operasyon, hijyen ve dayanıklılık dengesini ele alan yeni değerlendirmemiz.", image: "/images/gallery/showroom-2026-08-11/TEK_9586.jpg", alt: "Profesyonel mutfak tasarım detayı" }
];
export async function generateMetadata(): Promise<Metadata> { try { return managedMetadata(await getContentPage("basin-odasi"), "/basin-odasi"); } catch { return { title: "Basın Odası", robots: { index: false, follow: false } }; } }
function mapRelease(item: PublicPressRelease, index: number): PressViewItem { return { id: item.id, title: item.title, summary: item.summary, date: item.publishedAt, image: item.coverImageUrl ?? demoNews[index % demoNews.length].image, alt: item.coverAltText ?? item.title, category: "Kurumsal", href: item.attachmentUrl }; }
export default async function PressRoomPage() { let releases: PublicPressRelease[] = []; try { releases = await getPressReleases(); } catch { /* Demo records remain visible until press API is available. */ } const items = releases.length ? releases.map(mapRelease) : demoNews; return <main className={styles.page} data-public-page><PublicHeader currentPath="/basin-odasi" /><PressShowcase items={items} /><p className={styles.note}>Not: API’den içerik gelmediğinde gösterilen kayıtlar tasarım demosudur.</p></main>; }



