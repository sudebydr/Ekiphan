import Link from "next/link";
import styles from "./catalog-list.module.css";

export type FilterOption = {
  value: string;
  label: string;
  href: string;
  selected: boolean;
};

/** Açılınca seçenekleri alt alta listeleyen filtre grubu (açılır kutu/select yok). */
export function FilterOptionGroup({
  title,
  options,
  open
}: {
  title: string;
  options: FilterOption[];
  open?: boolean;
}) {
  const current = options.find((option) => option.selected && option.value !== "");
  return (
    <details className={styles.filterGroup} open={open ?? Boolean(current)}>
      <summary>
        <span>{title}</span>
        {current && <span className={styles.filterCurrent}>{current.label}</span>}
      </summary>
      <ul className={styles.optionList}>
        {options.map((option) => (
          <li key={option.value || "all"}>
            <Link
              className={`${styles.option} ${option.selected ? styles.optionSelected : ""}`}
              href={option.href}
              aria-current={option.selected ? "true" : undefined}
              scroll={false}
              prefetch={false}
            >
              {option.label}
            </Link>
          </li>
        ))}
      </ul>
    </details>
  );
}
