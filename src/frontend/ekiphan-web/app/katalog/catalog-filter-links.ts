/** /katalog filtre bağlantılarını üreten saf yardımcılar (sunucu ve istemci bileşenleri paylaşır). */

type SingleKey = "section" | "category" | "brand" | "tag" | "sort";

function finish(next: URLSearchParams): string {
  next.delete("page");
  next.delete("pageSize");
  const queryString = next.toString();
  return queryString ? `/katalog?${queryString}` : "/katalog";
}

/** Tek değerli filtre (grup, kategori, marka, etiket, sıralama) için bağlantı. Boş değer = filtreyi kaldır. */
export function optionHref(baseQuery: string, key: SingleKey, value: string): string {
  const next = new URLSearchParams(baseQuery);
  next.delete(key);
  if (value) next.set(key, value);
  // Özellik filtreleri kategoriye bağlı olduğundan kategori değişince sıfırlanır.
  if (key === "category") next.delete("attribute");
  return finish(next);
}

/** Dinamik özellik filtresi için bağlantı: aynı özelliğin eski seçimi değişir, diğerleri korunur. */
export function attributeOptionHref(baseQuery: string, attributeId: string, token: string): string {
  const next = new URLSearchParams(baseQuery);
  const keep = next.getAll("attribute").filter((item) => !item.startsWith(`${attributeId}:`));
  next.delete("attribute");
  for (const item of keep) next.append("attribute", item);
  if (token) next.append("attribute", `${attributeId}:${token}`);
  return finish(next);
}
