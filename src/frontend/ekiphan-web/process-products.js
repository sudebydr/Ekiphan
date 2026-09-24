const fs = require('fs');
const path = require('path');

const rawData = JSON.parse(fs.readFileSync('products.json', 'utf8'));

const imageDirs = [
  '../../../documents/bonna-gorsel1',
  '../../../documents/nude-gorsel',
  '../../../documents/pasabahce_gorsel'
];

const destDir = 'public/images/products';
if (!fs.existsSync(destDir)) {
  fs.mkdirSync(destDir, { recursive: true });
}

function findImage(code) {
  if (!code) return null;
  const safeCode = String(code).replace(/\//g, '-');
  for (const dir of imageDirs) {
    if (!fs.existsSync(dir)) continue;
    const files = fs.readdirSync(dir);
    for (const file of files) {
      if (file.toLowerCase().includes(safeCode.toLowerCase())) {
        return { dir, file };
      }
    }
  }
  return null;
}

const products = [];
let idCounter = 1;

for (const row of rawData) {
  const code = row['ürün kodu'];
  const name = row['ürün adı'] || code;
  if (!code) continue;

  const match = findImage(code);
  let imageUrl = null;
  if (match) {
    const srcPath = path.join(match.dir, match.file);
    const destPath = path.join(destDir, match.file);
    fs.copyFileSync(srcPath, destPath);
    imageUrl = `/images/products/${match.file}`;
  }

  // Parse categories
  const categoryRaw = row['ana kategori'] || '';
  const subCategoryRaw = row['1. alt kategori'] || '';
  
  // check if Mutfak
  let section = 'katalog';
  if (categoryRaw.toLowerCase().includes('mutfak') || subCategoryRaw.toLowerCase().includes('mutfak')) {
    section = 'mutfak';
  }

  products.push({
    id: String(idCounter++),
    slug: code.toLowerCase().replace(/[^a-z0-9]+/g, '-'),
    name: name,
    code: code,
    summary: row['kısa açıklama'] || '',
    coverImageUrl: imageUrl,
    section: section,
    brand: row['marka'] || null,
    categories: [categoryRaw, subCategoryRaw].filter(Boolean)
  });
}

fs.writeFileSync('frontend-products.json', JSON.stringify(products, null, 2));
console.log('Processed', products.length, 'products');
