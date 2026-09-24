const xlsx = require('xlsx');
const fs = require('fs');

const workbook = xlsx.readFile('../../../documents/Web_Veri_Hazirlik_Dosyasi_Version1 (1).xlsx');
const sheet_name_list = workbook.SheetNames;
const xlData = xlsx.utils.sheet_to_json(workbook.Sheets[sheet_name_list[0]]);

fs.writeFileSync('products.json', JSON.stringify(xlData, null, 2));
console.log('Saved to products.json');
