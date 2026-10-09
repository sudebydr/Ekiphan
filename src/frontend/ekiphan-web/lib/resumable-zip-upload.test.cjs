const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");
const ts = require("typescript");
const { webcrypto } = require("node:crypto");

test("five successive ZIP previews remain accessible without implicit DELETE", async () => {
  const { api, requests } = client();
  for (let i = 0; i < 5; i++) {
    const uploaded = await api.uploadZip(new File([new Uint8Array([i])], `preview-${i}.zip`), "product", () => {});
    assert.equal(uploaded.preview.files[0], "ok");
  }
  assert.equal(requests.filter(item => item[1] === "DELETE").length, 0);
  assert.equal(requests.filter(item => item[0].endsWith("zip-uploads") && item[1] === "POST").length, 5);
});

test("product validation uses owner-bound durable ZIP session, not expired preview token", async () => {
  const { api, state, requests } = client();
  const result = await api.processZip("upload", "validate", undefined, () => {});
  assert.equal(state.operation, "validate");
  assert.equal(result.files[0], "ok");
  assert.ok(requests.some(item => item[0] === "/api/admin/zip-uploads/upload/process"));
  assert.ok(!requests.some(item => item[0].includes("product-media-import/validate")));
});

function client({ interrupt = false, existing = null, fatal = false, storage = new Map(), closeStatus = 204, errorCode = null } = {}) {
  const state = existing ?? { id: "upload", offset: 0, length: 0, phase: "uploading", operation: null, chunks: [] };
  const requests = [];
  let failed = false;
  const context = { exports: {}, Blob, File, crypto: webcrypto, Uint8Array,
    setTimeout: callback => setTimeout(callback, 0),
    localStorage: { getItem: key => storage.get(key) ?? (existing ? "upload" : null), setItem: (key, value) => storage.set(key, value),
      get length() { return storage.size; }, key: index => [...storage.keys()][index], removeItem: key => storage.delete(key) },
    fetch: async (url, init) => {
      requests.push([url, init?.method ?? "GET", init?.body]);
      if (init?.method === "DELETE") return { ok: closeStatus === 204, status: closeStatus,
        json: async () => ({ detail: "Aktif aktarım kapatılamaz.", code: "UPLOAD_BUSY" }) };
      if (errorCode) return { ok: false, status: 409, json: async () => ({ detail: "Kota sınırı", code: errorCode }) };
      if (init?.method === "POST" && url.endsWith("zip-uploads")) {
        state.length = JSON.parse(init.body).length;
        state.offset = 0; state.phase = "uploading"; state.operation = null; state.chunks = [];
      } else if (url.endsWith("/process")) {
        state.phase = "completed"; state.operation = JSON.parse(init.body).operation; state.result = { files: ["ok"] };
      }
      return { ok: true, status: 200, json: async () => structuredClone(state) };
    },
    XMLHttpRequest: class {
      upload = {};
      open(method, url) { this.url = url; }
      setRequestHeader(name, value) { if (name === "X-Chunk-SHA256") this.hash = value; }
      async send(blob) {
        requests.push([this.url, "PUT"]);
        const offset = Number(this.url.split("=")[1]);
        if (!state.chunks.some(chunk => chunk.offset === offset)) {
          state.chunks.push({ offset, length: blob.size, sha256: this.hash }); state.offset += blob.size;
        }
        this.upload.onprogress({ loaded: blob.size });
        if (interrupt && !failed) { failed = true; this.onerror(); }
        else { this.status = fatal ? 422 : 200; this.responseText = '{"detail":"invalid"}'; this.onload(); }
      }
    }
  };
  const source = fs.readFileSync(__dirname + "/resumable-zip-upload.ts", "utf8");
  vm.runInNewContext(ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText, context);
  return { api: context.exports, state, requests, storage };
}

test("lost acknowledgement retries only one chunk; processing has its own phase", async () => {
  const { api, requests } = client({ interrupt: true });
  const phases = [];
  const file = new File([new Uint8Array(4 * 1024 * 1024 + 12)], "test.zip");
  const result = await api.uploadZip(file, "catalog", value => phases.push(value));
  assert.equal(result.preview.files[0], "ok");
  assert.equal(requests.filter(item => item[1] === "PUT").length, 3);
  assert.ok(phases.some(value => value.phase === "transfer" && value.percent === 100));
  assert.ok(phases.some(value => value.phase === "processing"));
});

test("resume verifies saved prefix and sends only missing bytes", async () => {
  const bytes = new Uint8Array(1024 * 1024 + 12);
  const hash = Buffer.from(await webcrypto.subtle.digest("SHA-256", bytes.slice(0, 1024 * 1024))).toString("hex").toUpperCase();
  const { api, requests } = client({ existing: { id: "upload", length: bytes.length, offset: 1024 * 1024,
    phase: "uploading", operation: null, chunks: [{ offset: 0, length: 1024 * 1024, sha256: hash }] } });
  await api.uploadZip(new File([bytes], "test.zip"), "product", () => {});
  assert.equal(requests.filter(item => item[1] === "PUT").length, 1);
  assert.ok(requests.some(item => item[0].endsWith("offset=1048576")));
});

test("permanent validation error is not retried", async () => {
  const { api, requests } = client({ fatal: true });
  await assert.rejects(api.uploadZip(new File([new Uint8Array(12)], "test.zip"), "catalog", () => {}));
  assert.equal(requests.filter(item => item[1] === "PUT").length, 1);
  assert.ok(!requests.some(item => item[0].endsWith("/process")));
});

test("wrong resumed file cannot replace acknowledged bytes", async () => {
  const { api, requests } = client({ existing: { id: "upload", length: 12, offset: 12,
    phase: "uploading", operation: null, chunks: [{ offset: 0, length: 12, sha256: "wrong" }] } });
  await assert.rejects(api.uploadZip(new File([new Uint8Array(12)], "test.zip"), "catalog", () => {}), /aynı değil/);
  assert.equal(requests.filter(item => item[1] === "PUT").length, 0);
});

test("manual close removes only matching ZIP pointers, leaving other resumes untouched", async () => {
  const storage = new Map([["ekiphan-zip-product-one", "upload"], ["ekiphan-zip-product-two", "other"], ["unrelated", "upload"]]);
  const { api, requests } = client({ storage });
  await api.closeZipUpload("upload");
  assert.equal(storage.has("ekiphan-zip-product-one"), false);
  assert.equal(storage.get("ekiphan-zip-product-two"), "other");
  assert.equal(storage.get("unrelated"), "upload");
  assert.equal(requests[0][1], "DELETE");
});

test("busy close failure preserves local resume pointer and stable reason code", async () => {
  const storage = new Map([["ekiphan-zip-product-one", "upload"]]);
  const { api } = client({ storage, closeStatus: 409 });
  await assert.rejects(api.closeZipUpload("upload"), error => error.code === "UPLOAD_BUSY");
  assert.equal(storage.get("ekiphan-zip-product-one"), "upload");
});

test("different file selection never automatically deletes another upload", async () => {
  const storage = new Map([["ekiphan-zip-product-other", "other"]]);
  const { api, requests } = client({ storage });
  await api.uploadZip(new File([new Uint8Array(12)], "new.zip"), "product", () => {});
  assert.equal(storage.get("ekiphan-zip-product-other"), "other");
  assert.equal(requests.some(request => request[1] === "DELETE"), false);
});

test("quota response is distinguishable and session callback preserves partial upload control", async () => {
  const { api } = client({ errorCode: "USER_UPLOAD_QUOTA" });
  await assert.rejects(api.listZipUploads(), error => error.code === "USER_UPLOAD_QUOTA");
  const partial = client({ fatal: true });
  let id;
  await assert.rejects(partial.api.uploadZip(new File([new Uint8Array(12)], "test.zip"), "product", () => {}, value => { id = value; }));
  assert.equal(id, "upload");
});

test("single PDF reuses chunks/retry and preserves title and replacement target", async () => {
  const { api, requests, state } = client({ interrupt: true });
  const fields = { title: "Catalog", replaceId: "existing-catalog" };
  const file = new File([new Uint8Array(4 * 1024 * 1024 + 12)], "catalog.pdf");
  await api.uploadSingle(file, "pdf", fields, () => {});
  assert.equal(state.operation, "execute");
  assert.equal(requests.filter(r => r[1] === "PUT").length, 3);
  const process = requests.find(r => r[0].endsWith("/process"));
  const command = JSON.parse(process[2]);
  assert.deepEqual(command.fields, fields);
  assert.equal(command.confirmed, true);
  await api.uploadSingle(file, "pdf", fields, () => {});
  assert.equal(requests.filter(r => r[1] === "PUT").length, 3);
});
test("single PDF can execute with only a file and no catalog selection or title", async () => {
  const { api, requests } = client();
  await api.uploadSingle(new File([new Uint8Array(12)], "fabrika-2026.pdf"), "pdf", {}, () => {});
  const create = JSON.parse(requests.find(r => r[0].endsWith("zip-uploads") && r[1] === "POST")[2]);
  assert.equal(create.kind, "pdf");
  assert.equal(create.fileName, "fabrika-2026.pdf");
  const process = JSON.parse(requests.find(r => r[0].endsWith("/process"))[2]);
  assert.equal(process.operation, "execute");
  assert.deepEqual(process.fields, {});
});
test("single image needs only filename and uses resumable execute without metadata fields", async () => {
  const { api, requests } = client();
  const fields = {};
  await api.uploadSingle(new File([new Uint8Array(12)], "4160.BRD.02TP02.webp"), "image", fields, () => {});
  const create = JSON.parse(requests.find(r => r[0].endsWith("zip-uploads") && r[1] === "POST")[2]);
  assert.equal(create.kind, "image");
  assert.equal(create.fileName, "4160.BRD.02TP02.webp");
  const process = JSON.parse(requests.find(r => r[0].endsWith("/process"))[2]);
  assert.equal(process.operation, "execute");
  assert.deepEqual(process.fields, fields);
});
test("RAR transfer uses the same verified chunk and preview protocol", async () => {
  const { api, requests } = client();
  await api.uploadZip(new File([new Uint8Array(12)], "images.rar"), "product", () => {});
  assert.equal(requests.filter(r => r[1] === "PUT").length, 1);
  assert.ok(requests.some(r => r[0].endsWith("/process")));
});
