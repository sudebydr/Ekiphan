const fs = require('fs');

async function main() {
  const filePath = '../../../documents/Web_Veri_Hazirlik_Dosyasi_Version1 (1).xlsx';
  const fileData = fs.readFileSync(filePath);

  const formData = new FormData();
  const blob = new Blob([fileData], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
  formData.append('file', blob, 'import.xlsx');

  try {
    const res = await fetch('http://localhost:5175/api/admin/product-import/preview', {
      method: 'POST',
      body: formData
    });
    console.log('Status:', res.status);
    const json = await res.json();
    console.log(JSON.stringify(json, null, 2));
    
    // Now trigger validate and execute using the token from preview
    if (json.token) {
        // We'd map columns here
        console.log('Got token:', json.token);
    }
  } catch(e) {
    console.error(e);
  }
}

main();
