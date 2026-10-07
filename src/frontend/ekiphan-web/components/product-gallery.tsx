"use client";

import { useEffect, useRef, useState, type CSSProperties, type PointerEvent } from "react";

type Image = { id: string; url: string; altText: string };

export function ProductGallery({ images, fallbackText }: { images: Image[]; fallbackText: string }) {
  const [selected, setSelected] = useState(0);
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [isZoomed, setIsZoomed] = useState(false);
  const [zoomOrigin, setZoomOrigin] = useState("50% 50%");
  const [trimmedUrl, setTrimmedUrl] = useState<string | null>(null);
  const trimmedUrlRef = useRef<string | null>(null);
  const active = images[selected];

  useEffect(() => { setIsZoomed(false); }, [selected]);
  useEffect(() => {
    if (!lightboxOpen) return;
    function closeOnEscape(event: KeyboardEvent) { if (event.key === "Escape") setLightboxOpen(false); }
    window.addEventListener("keydown", closeOnEscape);
    return () => window.removeEventListener("keydown", closeOnEscape);
  }, [lightboxOpen]);

  useEffect(() => {
    const sourceUrl = active?.url;
    let cancelled = false;
    setTrimmedUrl(null);
    if (trimmedUrlRef.current) {
      URL.revokeObjectURL(trimmedUrlRef.current);
      trimmedUrlRef.current = null;
    }
    if (!sourceUrl || new URL(sourceUrl, window.location.href).origin !== window.location.origin) return;

    const image = new Image();
    image.onload = () => {
      try {
        const canvas = document.createElement("canvas");
        canvas.width = image.naturalWidth;
        canvas.height = image.naturalHeight;
        const context = canvas.getContext("2d", { willReadFrequently: true });
        if (!context || !canvas.width || !canvas.height) return;
        context.drawImage(image, 0, 0);
        const pixels = context.getImageData(0, 0, canvas.width, canvas.height).data;
        let left = canvas.width, top = canvas.height, right = -1, bottom = -1;
        for (let y = 0; y < canvas.height; y++) for (let x = 0; x < canvas.width; x++) {
          if (pixels[(y * canvas.width + x) * 4 + 3] > 8) {
            left = Math.min(left, x); right = Math.max(right, x);
            top = Math.min(top, y); bottom = Math.max(bottom, y);
          }
        }
        if (right < left || bottom < top) return;
        const padding = 3;
        const cropLeft = Math.max(0, left - padding), cropTop = Math.max(0, top - padding);
        const cropRight = Math.min(canvas.width - 1, right + padding), cropBottom = Math.min(canvas.height - 1, bottom + padding);
        const cropWidth = cropRight - cropLeft + 1, cropHeight = cropBottom - cropTop + 1;
        if (cropWidth >= canvas.width * 0.96 && cropHeight >= canvas.height * 0.96) return;
        const cropped = document.createElement("canvas");
        cropped.width = cropWidth; cropped.height = cropHeight;
        cropped.getContext("2d")?.drawImage(canvas, cropLeft, cropTop, cropWidth, cropHeight, 0, 0, cropWidth, cropHeight);
        cropped.toBlob((blob) => {
          if (!blob || cancelled) return;
          const url = URL.createObjectURL(blob);
          trimmedUrlRef.current = url;
          setTrimmedUrl(url);
        }, "image/png");
      } catch { /* Tainted/unsupported canvas: keep the original asset. */ }
    };
    image.src = sourceUrl;
    return () => { cancelled = true; };
  }, [active?.url]);

  function updateZoomOrigin(event: PointerEvent<HTMLButtonElement>) {
    if (event.pointerType !== "mouse") return;
    const bounds = event.currentTarget.getBoundingClientRect();
    const x = Math.max(0, Math.min(100, ((event.clientX - bounds.left) / bounds.width) * 100));
    const y = Math.max(0, Math.min(100, ((event.clientY - bounds.top) / bounds.height) * 100));
    setZoomOrigin(`${x}% ${y}%`);
    setIsZoomed(true);
  }

  return <section className={images.length > 1 ? "productGallery" : "productGallery productGallerySingle"} aria-label="Ürün görsel galerisi">
    <div className="productGalleryMain productGalleryDetailViewport">
      {active ? <button className={isZoomed ? "productGalleryMainButton productGalleryZoomed" : "productGalleryMainButton"} type="button" onPointerMove={updateZoomOrigin} onPointerLeave={() => setIsZoomed(false)} onClick={() => setLightboxOpen(true)} aria-label="Ürün görselini tam ekran aç">
        <img src={trimmedUrl ?? active.url} alt={active.altText} width={960} height={960} fetchPriority="high" style={{ "--zoom-origin": zoomOrigin } as CSSProperties} />
      </button> : <span className="productGalleryFallback" aria-hidden="true">{fallbackText}</span>}
    </div>
    {images.length > 1 && <div className="productGalleryThumbnails" role="list" aria-label="Ürün görselleri">{images.map((image, index) => <button key={image.id} type="button" className={index === selected ? "productGalleryThumb productGalleryThumbSelected" : "productGalleryThumb"} onClick={() => setSelected(index)} aria-label={`${index + 1}. görseli göster`} aria-pressed={index === selected}><img src={image.url} alt="" width={80} height={80} /></button>)}</div>}
    {lightboxOpen && active && <div className="productGalleryLightbox" role="dialog" aria-modal="true" aria-label="Ürün görseli tam ekran görünümü" onClick={() => setLightboxOpen(false)}><img src={active.url} alt={active.altText} width={1600} height={1600} /><button className="productGalleryLightboxClose" type="button" onClick={() => setLightboxOpen(false)} aria-label="Tam ekran görünümü kapat">×</button></div>}
  </section>;
}

