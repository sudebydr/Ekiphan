import Link from "next/link";
import { getCatalogNavigation } from "../lib/catalog-api";
import styles from "./public-navigation.module.css";

type MegaGroup = {
  id: string;
  name: string;
  slug: string;
  children: {
    id: string;
    name: string;
    slug: string;
  }[];
};

async function loadGroups(): Promise<MegaGroup[]> {
  try {
    const navigation = await getCatalogNavigation();

    const roots = navigation.categories.filter(
      (item) => item.parentId === null
    );

    return roots
      .map((root) => ({
        id: root.id,
        name: root.name,
        slug: root.slug,
        children: navigation.categories
          .filter((item) => item.parentId === root.id)
          .map((child) => ({
            id: child.id,
            name: child.name,
            slug: child.slug
          }))
      }))
      .filter((group) => group.name);
  } catch {
    return [];
  }
}

export async function ProductsMegaMenu() {
  const groups = await loadGroups();

  if (groups.length === 0) {
    return (
      <div
        className={`${styles.productsSubmenu} ${styles.productsMegaPanel}`}
      >
        <div className={styles.productsMegaEmpty}>
          <div>
            <span className={styles.productsMegaEyebrow}>
              EKİPHAN KATALOĞU
            </span>

            <h3>Ürünlerimizi keşfedin</h3>

            <p>
              Profesyonel mutfak ve masaüstü çözümlerimizi
              incelemek için kataloğa göz atın.
            </p>
          </div>

          <Link
            className={styles.productsMegaAll}
            href="/katalog"
          >
            Tüm Ürünler
            <span aria-hidden="true">↗</span>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div
      className={`${styles.productsSubmenu} ${styles.productsMegaPanel}`}
    >
      <div className={styles.productsMegaGrid}>
        {groups.map((group) => (
          <div
            className={styles.productsMegaColumn}
            key={group.id}
          >
            <Link
              className={styles.productsMegaHeading}
              href={`/katalog?category=${encodeURIComponent(
                group.slug
              )}`}
            >
              {group.name}
            </Link>

            {group.children.length > 0 && (
              <ul>
                {group.children.map((child) => (
                  <li key={child.id}>
                    <Link
                      href={`/katalog?category=${encodeURIComponent(
                        child.slug
                      )}`}
                    >
                      {child.name}
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </div>
        ))}
      </div>

      <Link
        className={styles.productsMegaAll}
        href="/katalog"
      >
        Tüm Ürünler
        <span aria-hidden="true">↗</span>
      </Link>
    </div>
  );
}