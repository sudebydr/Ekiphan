import type {
  CatalogNavigation,
  CatalogPagedResult,
  CatalogProductDetail,
  CatalogProductSummary
} from "./catalog-types";

export type DemoTechnicalSpecification = {
  name: string;
  value: string;
};

export type DemoProduct = {
  id: string;
  sku: string;
  slug: string;
  name: string;
  brand: string;
  category: string;
  subCategory: string;
  shortDescription: string;
  longDescription: string;
  images: string[];
  technicalSpecifications: DemoTechnicalSpecification[];
  tags: string[];
  color: string;
  size: string;
  material: string;
  isActive: boolean;
};

const productImages = "/images/products/";
const genericImage = "/images/catalog-products-hero-v1.png";

export const demoProducts: DemoProduct[] = [
  {
    id: "demo-pbc-10355", sku: "3010.PBC.10355", slug: "pasabahce-10355-su-bardagi", name: "Bistro Su Barda\u011f\u0131", brand: "Pa\u015fabah\u00e7e", category: "Bardaklar ve Kadehler", subCategory: "Su Barda\u011f\u0131", shortDescription: "G\u00fcnl\u00fck servis i\u00e7in yal\u0131n ve dayan\u0131kl\u0131 cam bardak.", longDescription: "Restoran, otel ve kafe kullan\u0131m\u0131nda dengeli tut\u015fu ve net sunumu bir araya getiren profesyonel su barda\u011f\u0131.", images: [productImages + "3010.PBC.10355-1.webp", productImages + "3010.PBC.10355-2.webp"], technicalSpecifications: [{ name: "Marka", value: "Pa\u015fabah\u00e7e" }, { name: "Model", value: "Bistro 10355" }, { name: "Materyal", value: "Cam" }, { name: "Renk", value: "\u015eeffaf" }, { name: "Kapasite", value: "420 ml" }, { name: "\u00c7ap", value: "75 mm" }, { name: "Y\u00fckseklik", value: "145 mm" }], tags: ["Su servisi", "Restoran", "Kafe"], color: "\u015eeffaf", size: "420 ml", material: "Cam", isActive: true
  },
  {
    id: "demo-pbc-10356", sku: "3010.PBC.10356", slug: "pasabahce-10356-mesrubat-bardagi", name: "Me\u015frubat Barda\u011f\u0131", brand: "Pa\u015fabah\u00e7e", category: "Bardaklar ve Kadehler", subCategory: "Me\u015frubat Barda\u011f\u0131", shortDescription: "So\u011fuk i\u00e7ecek sunumlar\u0131 i\u00e7in geni\u015f hacimli cam bardak.", longDescription: "Yo\u011fun servis temposu i\u00e7in tasarlanan me\u015frubat barda\u011f\u0131, masada modern ve kullan\u0131\u015fl\u0131 bir sunum sa\u011flar.", images: [productImages + "3010.PBC.10356.webp"], technicalSpecifications: [{ name: "Marka", value: "Pa\u015fabah\u00e7e" }, { name: "Materyal", value: "Cam" }, { name: "Renk", value: "\u015eeffaf" }, { name: "Kapasite", value: "500 ml" }, { name: "Y\u00fckseklik", value: "155 mm" }], tags: ["So\u011fuk i\u00e7ecek", "Bar"], color: "\u015eeffaf", size: "500 ml", material: "Cam", isActive: true
  },
  {
    id: "demo-pbc-10531", sku: "3010.PBC.10531", slug: "pasabahce-10531-viski-bardagi", name: "Viski Barda\u011f\u0131", brand: "Pa\u015fabah\u00e7e", category: "Bardaklar ve Kadehler", subCategory: "Viski Barda\u011f\u0131", shortDescription: "Klasik viski ve kokteyl servisleri i\u00e7in al\u00e7ak form.", longDescription: "Bar servisinde dengeli kavrama sunan, buzlu i\u00e7eceklerle uyumlu profesyonel viski barda\u011f\u0131.", images: [productImages + "3010.PBC.10531.webp"], technicalSpecifications: [{ name: "Marka", value: "Pa\u015fabah\u00e7e" }, { name: "Materyal", value: "Cam" }, { name: "Renk", value: "\u015eeffaf" }, { name: "Kapasite", value: "320 ml" }, { name: "\u00c7ap", value: "82 mm" }], tags: ["Bar", "Kokteyl", "Viski"], color: "\u015eeffaf", size: "320 ml", material: "Cam", isActive: true
  },
  {
    id: "demo-pbc-10539", sku: "3010.PBC.10539", slug: "pasabahce-10539-shot-bardagi", name: "Shot Barda\u011f\u0131", brand: "Pa\u015fabah\u00e7e", category: "Bardaklar ve Kadehler", subCategory: "Shot Barda\u011f\u0131", shortDescription: "H\u0131zl\u0131 ve kontroll\u00fc shot servisi i\u00e7in kompakt tasar\u0131m.", longDescription: "Bar ve etkinlik servislerinde kullan\u0131lmak \u00fczere sade, dayan\u0131kl\u0131 ve istiflenebilir bir bardak alternatifi.", images: [productImages + "3010.PBC.10539.webp"], technicalSpecifications: [{ name: "Marka", value: "Pa\u015fabah\u00e7e" }, { name: "Materyal", value: "Cam" }, { name: "Renk", value: "\u015eeffaf" }, { name: "Kapasite", value: "60 ml" }, { name: "Y\u00fckseklik", value: "70 mm" }], tags: ["Bar", "Shot", "Etkinlik"], color: "\u015eeffaf", size: "60 ml", material: "Cam", isActive: true
  },
  {
    id: "demo-pbc-59001", sku: "3010.PBC.59001", slug: "pasabahce-59001-servis-kasesi", name: "Cam Servis Kasesi", brand: "Pa\u015fabah\u00e7e", category: "Masa\u00fcst\u00fc Sunum", subCategory: "Kase", shortDescription: "Tatlı, meze ve atıştırmalık sunumları için cam kase.", longDescription: "Profesyonel açık büfe ve masaüstü sunumlarında çok yönlü kullanılabilen şeffaf servis kasesi.", images: [productImages + "3010.PBC.59001-1.webp", productImages + "3010.PBC.59001-2.webp"], technicalSpecifications: [{ name: "Marka", value: "Pa\u015fabah\u00e7e" }, { name: "Materyal", value: "Cam" }, { name: "Renk", value: "Şeffaf" }, { name: "Ölçü", value: "15 cm" }, { name: "Kapasite", value: "650 ml" }], tags: ["Sunum", "Açık büfe", "Kase"], color: "Şeffaf", size: "15 cm", material: "Cam", isActive: true
  },
  {
    id: "demo-bnn-blm23ck", sku: "3111.BNN.BLM23CK", slug: "bonna-blm23ck-sunum-tabagi", name: "Bloom Sunum Tabağı", brand: "Bonna", category: "Masaüstü Sunum", subCategory: "Sunum Tabağı", shortDescription: "Şef sunumları için zarif porselen tabak.", longDescription: "Restoran sunumlarına karakter kazandıran Bonna porselen tabak, profesyonel kullanım için dayanıklı sır yüzeye sahiptir.", images: [productImages + "3111.BNN.BLM23CK-1.webp", productImages + "3111.BNN.BLM23CK-2.webp"], technicalSpecifications: [{ name: "Marka", value: "Bonna" }, { name: "Model", value: "Bloom BLM23CK" }, { name: "Materyal", value: "Porselen" }, { name: "Renk", value: "Krem" }, { name: "Ölçü", value: "23 cm" }, { name: "Çap", value: "230 mm" }], tags: ["Porselen", "Şef sunumu", "Restoran"], color: "Krem", size: "23 cm", material: "Porselen", isActive: true
  },
  {
    id: "demo-bnn-bnc01cf", sku: "3111.BNN.BNC01CF", slug: "bonna-bnc01cf-servis-tabagi", name: "Banquet Servis Tabağı", brand: "Bonna", category: "Masaüstü Sunum", subCategory: "Servis Tabağı", shortDescription: "Yoğun restoran servisi için dayanıklı porselen servis tabağı.", longDescription: "Klasik formu ve dayanıklı yapısıyla otel ve restoranların ana yemek servislerine uyum sağlayan profesyonel tabak.", images: [productImages + "3111.BNN.BNC01CF.webp"], technicalSpecifications: [{ name: "Marka", value: "Bonna" }, { name: "Materyal", value: "Porselen" }, { name: "Renk", value: "Beyaz" }, { name: "Ölçü", value: "27 cm" }, { name: "Çap", value: "270 mm" }], tags: ["Ana yemek", "Porselen", "Otel"], color: "Beyaz", size: "27 cm", material: "Porselen", isActive: true
  },
  {
    id: "demo-bnn-bnc01ct", sku: "3111.BNN.BNC01CT", slug: "bonna-bnc01ct-kase", name: "Banquet Porselen Kase", brand: "Bonna", category: "Masaüstü Sunum", subCategory: "Kase", shortDescription: "Çorba, tatlı ve küçük sunumlar için porselen kase.", longDescription: "Farklı servis senaryolarında kullanılabilen, sade çizgili ve istiflenebilir profesyonel porselen kase.", images: [productImages + "3111.BNN.BNC01CT.webp"], technicalSpecifications: [{ name: "Marka", value: "Bonna" }, { name: "Materyal", value: "Porselen" }, { name: "Renk", value: "Beyaz" }, { name: "Kapasite", value: "420 ml" }, { name: "Çap", value: "150 mm" }], tags: ["Kase", "Çorba", "Sunum"], color: "Beyaz", size: "420 ml", material: "Porselen", isActive: true
  },
  {
    id: "demo-bnn-grm21dz", sku: "3111.BNN.GRM21DZ", slug: "bonna-grm21dz-gourmet-tabak", name: "Gourmet Düz Tabak", brand: "Bonna", category: "Masaüstü Sunum", subCategory: "Düz Tabak", shortDescription: "Modern tabak sunumları için geniş düz yüzey.", longDescription: "Profesyonel mutfakların ana yemek ve şef sunumlarında kullandığı, estetik ve dayanıklı porselen düz tabak.", images: [productImages + "3111.BNN.GRM21DZ-1.webp", productImages + "3111.BNN.GRM21DZ-2.webp"], technicalSpecifications: [{ name: "Marka", value: "Bonna" }, { name: "Materyal", value: "Porselen" }, { name: "Renk", value: "Beyaz" }, { name: "Ölçü", value: "21 cm" }, { name: "Çap", value: "210 mm" }], tags: ["Gourmet", "Şef sunumu", "Porselen"], color: "Beyaz", size: "21 cm", material: "Porselen", isActive: true
  },
  {
    id: "demo-nude-01", sku: "DEMO.NUDE.001", slug: "nude-mixed-drink-bardagi", name: "Nude Mixology Bardağı", brand: "Nude", category: "Bardaklar ve Kadehler", subCategory: "Kokteyl Bardağı", shortDescription: "Kokteyl sunumları için ince cidarlı modern bardak.", longDescription: "Demo koleksiyonunda cam ürün grubu filtrelerini test etmek üzere hazırlanmış profesyonel kokteyl bardağı.", images: [genericImage], technicalSpecifications: [{ name: "Marka", value: "Nude" }, { name: "Materyal", value: "Cam" }, { name: "Renk", value: "Şeffaf" }, { name: "Kapasite", value: "350 ml" }], tags: ["Kokteyl", "Bar", "Cam"], color: "Şeffaf", size: "350 ml", material: "Cam", isActive: true
  },
  {
    id: "demo-nude-02", sku: "DEMO.NUDE.002", slug: "nude-sarap-kadehi", name: "Nude Şarap Kadehi", brand: "Nude", category: "Bardaklar ve Kadehler", subCategory: "Şarap Kadehi", shortDescription: "Şarap aromalarını öne çıkaran ince cam kadeh.", longDescription: "Şarap servis alanlarını temsil eden demo ürün; ürün detay galerisi, etiket ve teknik özellik görünümünü test eder.", images: [genericImage], technicalSpecifications: [{ name: "Marka", value: "Nude" }, { name: "Materyal", value: "Cam" }, { name: "Renk", value: "Şeffaf" }, { name: "Kapasite", value: "520 ml" }, { name: "Yükseklik", value: "225 mm" }], tags: ["Şarap", "Kadeh", "Otel"], color: "Şeffaf", size: "520 ml", material: "Cam", isActive: true
  },
  {
    id: "demo-equipment-01", sku: "DEMO.EKP.001", slug: "servis-tepsisi", name: "Profesyonel Servis Tepsisi", brand: "Ekiphan", category: "Servis Ekipmanları", subCategory: "Servis Tepsisi", shortDescription: "Kaydırmaz yüzeyli, yoğun servis için tepsi.", longDescription: "Kafe, restoran ve otel servis operasyonları için pratik ve dayanıklı servis ekipmanı demo ürünü.", images: [genericImage], technicalSpecifications: [{ name: "Marka", value: "Ekiphan" }, { name: "Materyal", value: "Kompozit" }, { name: "Renk", value: "Siyah" }, { name: "Ölçü", value: "40 cm" }], tags: ["Servis", "Tepsi", "Restoran"], color: "Siyah", size: "40 cm", material: "Kompozit", isActive: true
  }
];

export function isDemoProductsEnabled(): boolean {
  return process.env.NEXT_PUBLIC_USE_DEMO_PRODUCTS === "true";
}

function slugify(value: string): string {
  return value.toLocaleLowerCase("tr-TR").replace(/ı/g, "i").normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "");
}

function summary(product: DemoProduct): CatalogProductSummary {
  return { id: product.id, sku: product.sku, name: product.name, slug: product.slug, shortDescription: product.shortDescription, brand: { id: slugify(product.brand), name: product.brand, slug: slugify(product.brand) }, primaryCategory: { id: slugify(product.category), name: product.category, slug: slugify(product.category) }, updatedAt: "", image: { id: `${product.id}-image-1`, url: product.images[0] ?? genericImage, altText: product.name } };
}

export function getDemoCatalogNavigation(): CatalogNavigation {
  const categories = [...new Set(demoProducts.map((product) => product.category))];
  const brands = [...new Set(demoProducts.map((product) => product.brand))];
  const tags = [...new Set(demoProducts.flatMap((product) => product.tags))];
  return { sections: [{ id: "demo-katalog", code: "KATALOG", name: "Katalog", slug: "katalog" }], categories: categories.map((name) => ({ id: slugify(name), sectionId: "demo-katalog", parentId: null, name, slug: slugify(name), metaTitle: null, metaDescription: null, canonicalUrl: null, noIndex: true, noFollow: false, openGraphTitle: null, openGraphDescription: null, openGraphImageUrl: null })), brands: brands.map((name) => ({ id: slugify(name), name, slug: slugify(name) })), tags: tags.map((name) => ({ id: slugify(name), code: slugify(name).toUpperCase(), name, slug: slugify(name) })) };
}

export function getDemoProducts(query: URLSearchParams): CatalogPagedResult {
  const search = query.get("q")?.trim().toLocaleLowerCase("tr-TR") ?? "";
  const section = query.get("section") ?? "";
  const category = query.get("category") ?? "";
  const brand = query.get("brand") ?? "";
  const tag = query.get("tag") ?? "";
  const sort = query.get("sort") ?? "Name";
  const page = Math.max(1, Number(query.get("page") ?? "1") || 1);
  const pageSize = Math.max(1, Number(query.get("pageSize") ?? "24") || 24);
  let items = demoProducts.filter((product) => product.isActive && (!search || `${product.name} ${product.sku} ${product.brand}`.toLocaleLowerCase("tr-TR").includes(search)) && (!section || section === "katalog") && (!category || slugify(product.category) === category) && (!brand || slugify(product.brand) === brand) && (!tag || product.tags.some((item) => slugify(item) === tag)));
  items = [...items].sort((a, b) => (sort === "NameDescending" ? -1 : 1) * a.name.localeCompare(b.name, "tr"));
  return { items: items.slice((page - 1) * pageSize, page * pageSize).map(summary), page, pageSize, totalCount: items.length };
}

export function getDemoProduct(slug: string): CatalogProductDetail | null {
  const product = demoProducts.find((item) => item.slug === slug && item.isActive);
  if (!product) return null;
  const related = demoProducts.filter((item) => item.id !== product.id && item.category === product.category).slice(0, 3).map(summary);
  return { ...summary(product), longDescription: product.longDescription, categories: [{ id: slugify(product.category), name: product.category, slug: slugify(product.category) }, { id: slugify(product.subCategory), name: product.subCategory, slug: slugify(product.subCategory) }], tags: product.tags.map((name) => ({ id: slugify(name), code: slugify(name).toUpperCase(), name, slug: slugify(name) })), attributes: product.technicalSpecifications.filter((item) => item.value).map((item, index) => ({ id: `${product.id}-attribute-${index}`, attributeId: `${product.id}-attribute-${index}`, name: item.name, dataType: 0, sequence: index + 1, textValue: item.value, numericValue: null, booleanValue: null, optionId: null, optionName: null, unitId: null, unitSymbol: null })), similarProducts: related, complementaryProducts: [], images: product.images.map((url, index) => ({ id: `${product.id}-image-${index + 1}`, url, altText: `${product.name} görsel ${index + 1}` })), metaTitle: product.name, metaDescription: product.shortDescription, canonicalUrl: null, noIndex: true, noFollow: false, openGraphTitle: null, openGraphDescription: null, openGraphImageUrl: null, alternates: null };
}