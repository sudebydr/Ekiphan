"use client";

import { useEffect, useState } from "react";
import { addQuoteItem, quoteListChangedEvent, readQuoteList, writeQuoteList } from "../lib/quote-list";

type Props = {
  productId: string;
  slug: string;
  name: string;
  sku: string;
  brandName: string | null;
  imageUrl?: string | null;
  className: string;
  showQuantityControl?: boolean;
  quantityClassName?: string;
  compactQuantityControl?: boolean;
};

export function AddToQuoteButton({
  productId,
  slug,
  name,
  sku,
  brandName,
  imageUrl,
  className,
  showQuantityControl = false,
  quantityClassName,
  compactQuantityControl = false
}: Props) {
  const [message, setMessage] = useState("");
  const [quantity, setQuantity] = useState<number | null>(null);

  useEffect(() => {
    function syncQuantity() {
      const item = readQuoteList().find((entry) => entry.productId === productId);
      setQuantity(item?.quantity ?? null);
    }
    syncQuantity();
    window.addEventListener(quoteListChangedEvent, syncQuantity);
    return () => window.removeEventListener(quoteListChangedEvent, syncQuantity);
  }, [productId]);

  function add() {
    const result = addQuoteItem({ productId, slug, name, sku, brandName, imageUrl });
    if (result === "added" || result === "exists") {
      setQuantity(readQuoteList().find((entry) => entry.productId === productId)?.quantity ?? 1);
      setMessage("");
      return;
    }
    setMessage(result === "full" ? "Teklif listeniz en fazla 50 \u00fcr\u00fcn i\u00e7erebilir." : "Taray\u0131c\u0131n\u0131z teklif listesini kaydetmeye izin vermiyor.");
  }

  function changeQuantity(change: number) {
    const items = readQuoteList();
    const item = items.find((entry) => entry.productId === productId);
    if (!item) return;
    if (change < 0 && item.quantity === 1) {
      if (writeQuoteList(items.filter((entry) => entry.productId !== productId))) {
        setQuantity(null);
        setMessage("");
      }
      return;
    }
    const nextQuantity = Math.max(1, Math.min(100_000, item.quantity + change));
    if (nextQuantity === item.quantity) return;
    if (writeQuoteList(items.map((entry) => entry.productId === productId ? { ...entry, quantity: nextQuantity } : entry))) {
      setQuantity(nextQuantity);
    }
  }

  if (showQuantityControl && quantity !== null) {
    if (compactQuantityControl) {
      return (
        <div className={quantityClassName}>
          <div className="quoteQuantityControl" aria-label="Teklif listesi adedi">
            <button type="button" onClick={() => changeQuantity(-1)} aria-label="Adedi azalt">-</button>
            <output>{quantity}</output>
            <button type="button" onClick={() => changeQuantity(1)} aria-label={"Adedi art\u0131r"}>+</button>
          </div>
        </div>
      );
    }

    return (
      <div className={quantityClassName} aria-label="Teklif listesi adedi">
        <button type="button" onClick={() => changeQuantity(-1)} aria-label="Adedi azalt">-</button>
        <output>{quantity}</output>
        <button type="button" onClick={() => changeQuantity(1)} aria-label={"Adedi art\u0131r"}>+</button>
      </div>
    );
  }

  return (
    <div>
      <button className={className} type="button" onClick={add}>Teklif listeme ekle</button>
      {message && <p role="status">{message}</p>}
    </div>
  );
}