const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const ts = require("typescript");
const React = require("react");
const { renderToStaticMarkup } = require("react-dom/server");

function component(file, name) {
  const source = ts.transpileModule(fs.readFileSync(path.join(__dirname, file), "utf8"), {
    compilerOptions: { jsx: ts.JsxEmit.ReactJSX, module: ts.ModuleKind.CommonJS, esModuleInterop: true }
  }).outputText;
  const exports = {};
  const load = id => id.endsWith(".css") ? new Proxy({}, { get: (_, key) => key === "__esModule" ? false : key })
    : id === "next/link" ? ({ children, ...props }) => React.createElement("a", props, children) : require(id);
  new Function("require", "exports", source)(load, exports);
  return exports[name];
}

test("shared detail overview has no oversized slogan or empty feature block; TR/EN tabs remain", () => {
  const Sections = component("product-detail-sections.tsx", "ProductDetailSections");
  for (const locale of ["tr", "en"]) {
    const html = renderToStaticMarkup(React.createElement(Sections, { productName: "Test", category: "Category",
      description: "", features: [], specificationGroups: [], applicationAreas: [], locale }));
    assert.equal((html.match(/role="tab"/g) || []).length, 4);
    assert.doesNotMatch(html, /maksimum performans|Reliable performance|ek özellik bilgisi|additional product highlights/);
    assert.match(html, locale === "tr" ? /Teknik özellikler/ : /Specifications/);
  }
});

test("related carousel hides empty output and unnecessary controls, preserves linked equal-class cards", () => {
  const Carousel = component("related-products-carousel.tsx", "RelatedProductsCarousel");
  const items = Array.from({ length: 5 }, (_, index) => ({ id: String(index), slug: `product-${index}`,
    name: `Product ${index}`, sku: `SKU-${index}`, category: "Category", brand: "Brand" }));
  assert.equal(renderToStaticMarkup(React.createElement(Carousel, { items: [] })), "");
  const short = renderToStaticMarkup(React.createElement(Carousel, { items: items.slice(0, 2) }));
  assert.equal((short.match(/class="relatedCard"/g) || []).length, 2);
  assert.doesNotMatch(short, /Önceki ürünler|Sonraki ürünler/);
  const full = renderToStaticMarkup(React.createElement(Carousel, { items, locale: "en" }));
  assert.match(full, /Previous products/);
  assert.match(full, /href="\/en\/products\/product-0"/);
});

test("compact carousel sizing stays inside narrow and desktop viewports with consistent steps", () => {
  const layout = component("related-products-carousel.tsx", "relatedLayout");
  for (const width of [288, 320, 343, 375, 736, 768, 1280, 1366, 1408, 1920]) {
    const result = layout(width);
    assert.ok(result.cardWidth > 0 && result.cardWidth <= 280);
    assert.ok(result.perView * result.cardWidth + (result.perView - 1) * 16 <= width);
    assert.equal(result.step, result.cardWidth + 16);
  }
});

test("detail gallery neutralizes inherited padding and uses border-box inside its grid column", () => {
  const css = fs.readFileSync(path.join(__dirname, "../app/katalog/product-detail.module.css"), "utf8");
  assert.match(css, /\.galleryColumn,\.galleryColumn \*\{box-sizing:border-box\}/);
  assert.match(css, /:global\(\.productGallery\)\{[^}]*width:100%;max-width:100%;height:auto;padding:0/);
  assert.match(css, /:global\(\.productGalleryDetailViewport\)\{[^}]*overflow:hidden/);
  assert.match(css, /:global\(\.productGalleryMainButton img\)\{[^}]*object-fit:contain;object-position:center;transform:none/);
  assert.match(css, /:global\(\.productGalleryMainButton\.productGalleryZoomed img\)\{transform:scale\(2\.1\)\}/);
});

test("long product content and one related item preserve text, links and compact card structure", () => {
  const Sections = component("product-detail-sections.tsx", "ProductDetailSections");
  const name = "Uzun ürün adı ".repeat(30);
  const html = renderToStaticMarkup(React.createElement(Sections, { productName: name, category: "Category",
    description: "Açıklama", features: [{ title: "Özellik", detail: name }], specificationGroups: [], applicationAreas: [] }));
  assert.match(html, /Açıklama/);
  assert.match(html, /Özellik/);
  const Carousel = component("related-products-carousel.tsx", "RelatedProductsCarousel");
  const card = renderToStaticMarkup(React.createElement(Carousel, { items: [{ id: "one", slug: "one", name,
    sku: "SKU", category: "", brand: "", image: null }] }));
  assert.equal((card.match(/class="relatedCard"/g) || []).length, 1);
  assert.match(card, /href="\/katalog\/one"/);
  assert.doesNotMatch(card, /relatedArrow/);
});
