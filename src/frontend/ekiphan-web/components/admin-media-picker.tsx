"use client";

import { useEffect, useMemo, useState } from "react";
import type {
  AdminMediaAsset,
  AdminMediaLibrary
} from "../lib/admin-media-types";
import styles from "./admin-media-picker.module.css";

type MediaType = AdminMediaAsset["assetType"];

export function AdminMediaPicker({
  label,
  value,
  onChange,
  acceptedTypes,
  required = false
}: {
  label: string;
  value: string;
  onChange: (id: string) => void;
  acceptedTypes: MediaType[];
  required?: boolean;
}) {
  const [assets, setAssets] = useState<AdminMediaAsset[]>([]);
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void fetch("/api/admin/media-library", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) {
          const body = (await response.json()) as { detail?: string };
          throw new Error(body.detail ?? "Medya kütüphanesi alınamadı.");
        }
        const library = (await response.json()) as AdminMediaLibrary;
        setAssets(library.assets);
      })
      .catch((reason: unknown) =>
        setError(reason instanceof Error ? reason.message : "Medya kütüphanesi alınamadı.")
      );
  }, []);

  const available = useMemo(() => {
    const term = search.trim().toLocaleLowerCase("tr-TR");
    return assets.filter((asset) => {
      if (asset.status !== "Active" || !acceptedTypes.includes(asset.assetType)) {
        return false;
      }
      if (!term) return true;
      const text = [
        asset.originalFileName,
        ...asset.translations.flatMap((translation) => [
          translation.title,
          translation.altText
        ])
      ].filter(Boolean).join(" ").toLocaleLowerCase("tr-TR");
      return text.includes(term);
    });
  }, [acceptedTypes, assets, search]);
  const selected = assets.find((asset) => asset.id === value);
  const titleOf = (asset: AdminMediaAsset) =>
    asset.translations.find((translation) => translation.languageCode === "tr")?.title ??
    asset.originalFileName ??
    asset.id;

  return (
    <fieldset className={styles.picker}>
      <legend>{label}{required ? " *" : ""}</legend>
      {selected ? (
        <div className={styles.selected}>
          {selected.assetType === "Image" && selected.url ? (
            <img src={selected.url}
              alt={selected.translations.find((text) => text.languageCode === "tr")?.altText ?? ""}
              width={160} height={100} />
          ) : (
            <span className={styles.fileType}>{selected.assetType}</span>
          )}
          <div><strong>{titleOf(selected)}</strong>
            <small>{selected.originalFileName ?? selected.id}</small></div>
        </div>
      ) : value ? (
        <p className={styles.unknown}>Seçili medya: {value}</p>
      ) : (
        <p className={styles.empty}>Henüz medya seçilmedi.</p>
      )}
      <div className={styles.actions}>
        <button type="button" onClick={() => setOpen((current) => !current)}>
          {open ? "Seçiciyi kapat" : "Medya seç"}
        </button>
        {value && <button type="button" onClick={() => onChange("")}>Seçimi kaldır</button>}
      </div>
      {open && (
        <div className={styles.library}>
          <label>Medya ara
            <input type="search" value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Dosya adı, başlık veya alt metin" />
          </label>
          {error ? (
            <p className={styles.error} role="alert">{error}</p>
          ) : available.length === 0 ? (
            <p className={styles.empty}>Uygun aktif medya bulunamadı.</p>
          ) : (
            <div className={styles.grid}>
              {available.map((asset) => (
                <button type="button" key={asset.id}
                  aria-pressed={asset.id === value}
                  onClick={() => { onChange(asset.id); setOpen(false); }}>
                  {asset.assetType === "Image" && asset.url ? (
                    <img src={asset.url}
                      alt={asset.translations.find((text) => text.languageCode === "tr")?.altText ?? ""}
                      width={180} height={112} loading="lazy" />
                  ) : (
                    <span className={styles.fileType}>{asset.assetType}</span>
                  )}
                  <strong>{titleOf(asset)}</strong>
                  <small>{asset.originalFileName ?? asset.id}</small>
                </button>
              ))}
            </div>
          )}
        </div>
      )}
      <input className={styles.hiddenInput} tabIndex={-1} required={required}
        value={value} onChange={() => undefined} aria-hidden="true" />
    </fieldset>
  );
}
