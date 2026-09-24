"use client";

import { useEffect, useState, type CSSProperties, type PointerEvent } from "react";

type Image = { id: string; url: string; altText: string };

export function ProductGallery({ images, fallbackText }: { images: Image[]; fallbackText: string }) {
  const [selected, setSelected] = useState(0);
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [aspectRatio, setAspectRatio] = useState<number | null>(null);
  const [isZoomed, setIsZoomed] = useState(false);
  const [zoomOrigin, setZoomOrigin] = useState("50% 50%");
  const active = images[selected];

  useEffect(() => { setAspectRatio(null); setIsZoomed(false); }, [selected]);
  useEffect(() => {
    if (!lightboxOpen) return;
    function closeOnEscape(event: KeyboardEvent) { if (event.key === "Escape") setLightboxOpen(false); }
    window.addEventListener("keydown", closeOnEscape);
    return () => window.removeEventListener("keydown", closeOnEscape);
  }, [lightboxOpen]);

  function updateZoomOrigin(event: PointerEvent<HTMLButtonElement>) {
    if (event.pointerType !== "mouse") return;
    const bounds = event.currentTarget.getBoundingClientRect();
    const x = Math.max(0, Math.min(100, ((event.clientX - bounds.left) / bounds.width) * 100));
    const y = Math.max(0, Math.min(100, ((event.clientY - bounds.top) / bounds.height) * 100));
    setZoomOrigin(`${x}% ${y}%`);
    setIsZoomed(true);
  }

  return <section className={images.length > 1 ? "productGallery" : "productGallery productGallerySingle"} aria-label="Ürün görsel galerisi">
    <div className="productGalleryMain" style={aspectRatio ? { aspectRatio } : undefined}>
      {active ? <button className={isZoomed ? "productGalleryMainButton productGalleryZoomed" : "productGalleryMainButton"} type="button" onPointerMove={updateZoomOrigin} onPointerLeave={() => setIsZoomed(false)} onClick={() => setLightboxOpen(true)} aria-label="Ürün görselini tam ekran aç">
        <img src={active.url} alt={active.altText} width={960} height={960} fetchPriority="high" style={{ "--zoom-origin": zoomOrigin } as CSSProperties} onLoad={(event) => { const image = event.currentTarget; const ratio = image.naturalWidth / image.naturalHeight; setAspectRatio(Math.min(1.7, Math.max(0.85, ratio))); }} />
      </button> : <span className="productGalleryFallback" aria-hidden="true">{fallbackText}</span>}
    </div>
    {images.length > 1 && <div className="productGalleryThumbnails" role="list" aria-label="Ürün görselleri">{images.map((image, index) => <button key={image.id} type="button" className={index === selected ? "productGalleryThumb productGalleryThumbSelected" : "productGalleryThumb"} onClick={() => setSelected(index)} aria-label={`${index + 1}. görseli göster`} aria-pressed={index === selected}><img src={image.url} alt="" width={80} height={80} /></button>)}</div>}
    {lightboxOpen && active && <div className="productGalleryLightbox" role="dialog" aria-modal="true" aria-label="Ürün görseli tam ekran görünümü" onClick={() => setLightboxOpen(false)}><img src={active.url} alt={active.altText} width={1600} height={1600} /><button className="productGalleryLightboxClose" type="button" onClick={() => setLightboxOpen(false)} aria-label="Tam ekran görünümü kapat">×</button></div>}
  </section>;
}

