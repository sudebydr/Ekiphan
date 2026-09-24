import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { PublicHeader } from "../../../components/public-header";
import { PublicNavigation } from "../../../components/public-navigation";
import { CatalogApiError, getContentPage } from "../../../lib/catalog-api";
import { managedMetadata } from "../../../lib/managed-metadata";
import styles from "./page.module.css";

type Props = { params: Promise<{ slug: string }>; searchParams: Promise<{ lang?: string }> };

const languageOf = (value?: string): "tr" | "en" => value === "en" ? "en" : "tr";

async function load(slug: string, language: "tr" | "en") {
  try {
    return await getContentPage(slug, language);
  } catch (error) {
    if (error instanceof CatalogApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }
}

export async function generateMetadata({ params, searchParams }: Props): Promise<Metadata> {
  const page = await load((await params).slug, languageOf((await searchParams).lang));
  return managedMetadata(page, `/sayfa/${encodeURIComponent(page.slug)}`);
}

export default async function ManagedContentPage({ params, searchParams }: Props) {
  const page = await load((await params).slug, languageOf((await searchParams).lang));
  const paragraphs = page.body
    .split(/\r?\n{2,}/)
    .map((item) => item.trim())
    .filter(Boolean);

  return (
    <main className={styles.page} data-public-page>
      <PublicHeader />
      <section className={styles.hero}>
        <p className={styles.eyebrow}>EKİPHAN · KURUMSAL</p>
        <h1>{page.title}</h1>
        {page.summary && <p className={styles.summary}>{page.summary}</p>}
      </section>
      <nav className={styles.breadcrumb} aria-label="Breadcrumb">
        <Link href="/">Ana Sayfa</Link><span aria-hidden="true">/</span>
        <span>{page.title}</span>
      </nav>
      <article>
        <div className={styles.body}>
          {paragraphs.map((paragraph, index) => (
            <p key={`${index}-${paragraph.slice(0, 24)}`}>{paragraph}</p>
          ))}
        </div>
      </article>
    </main>
  );
}
