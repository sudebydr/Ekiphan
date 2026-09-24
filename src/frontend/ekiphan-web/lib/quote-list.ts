export const quoteStorageKey = "ekiphan_quote_list_v1";
export const quoteListChangedEvent = "ekiphan:quote-list-changed";
export const maximumQuoteItems = 50;

const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const demoProductIdPattern = /^demo-[a-z0-9-]{1,100}$/i;

export type LocalQuoteItem = {
  productId: string;
  slug: string;
  name: string;
  sku: string;
  brandName: string | null;
  imageUrl?: string | null;
  quantity: number;
  note: string;
};

function isQuoteItem(value: unknown): value is LocalQuoteItem {
  if (!value || typeof value !== "object") return false;
  const item = value as Record<string, unknown>;
  return (
    typeof item.productId === "string" &&
    (guidPattern.test(item.productId) || demoProductIdPattern.test(item.productId)) &&
    typeof item.slug === "string" &&
    item.slug.length <= 300 &&
    typeof item.name === "string" &&
    item.name.length <= 250 &&
    typeof item.sku === "string" &&
    item.sku.length <= 100 &&
    (item.brandName === null ||
      (typeof item.brandName === "string" &&
        item.brandName.length <= 150)) &&
    (item.imageUrl === undefined || item.imageUrl === null ||
      (typeof item.imageUrl === "string" && item.imageUrl.length <= 2_000)) &&
    Number.isInteger(item.quantity) &&
    Number(item.quantity) >= 1 &&
    Number(item.quantity) <= 100_000 &&
    typeof item.note === "string" &&
    item.note.length <= 2_000
  );
}

export function readQuoteList(): LocalQuoteItem[] {
  try {
    const raw = window.localStorage.getItem(quoteStorageKey);
    if (!raw) return [];
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) return [];
    return parsed.slice(0, maximumQuoteItems).filter(isQuoteItem);
  } catch {
    return [];
  }
}

export function writeQuoteList(items: LocalQuoteItem[]): boolean {
  try {
    const safeItems = items.slice(0, maximumQuoteItems).filter(isQuoteItem);
    window.localStorage.setItem(quoteStorageKey, JSON.stringify(safeItems));
    window.dispatchEvent(new Event(quoteListChangedEvent));
    return true;
  } catch {
    return false;
  }
}

export function addQuoteItem(
  item: Omit<LocalQuoteItem, "quantity" | "note">
): "added" | "exists" | "full" | "unavailable" {
  const items = readQuoteList();
  if (items.some((existing) => existing.productId === item.productId)) {
    return "exists";
  }
  if (items.length >= maximumQuoteItems) return "full";
  return writeQuoteList([
    ...items,
    { ...item, quantity: 1, note: "" }
  ])
    ? "added"
    : "unavailable";
}
