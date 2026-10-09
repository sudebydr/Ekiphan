export type UploadKind = "catalog" | "product" | "image" | "pdf";
export type ZipProgress = { percent: number; phase: "transfer" | "processing" | "ready"; message: string };
type State = { id: string; offset: number; length: number; phase: string; operation: string | null;
  result: unknown; error: string | null; chunks: { offset: number; length: number; sha256: string }[] };
const base = "/api/admin/zip-uploads";
const chunkBytes = 4 * 1024 * 1024;
const fingerprintBytes = 1024 * 1024;
const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms));

export type ZipUploadSession = { id: string; kind: UploadKind; fileName: string;
  length: number; offset: number; phase: string; operation: string | null; expiresAt: string; canClose: boolean };

export async function listZipUploads(): Promise<ZipUploadSession[]> {
  return json<ZipUploadSession[]>("");
}

export async function closeZipUpload(id: string): Promise<void> {
  const response = await fetch(`${base}/${id}`, { method: "DELETE", cache: "no-store" });
  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new HttpError(response.status, error.detail ?? "Yükleme kapatılamadı.", error.code);
  }
  // Remove only this successfully closed session's pointers, never other uploads.
  try {
    const keys: string[] = [];
    for (let index = 0; index < localStorage.length; index++) {
      const key = localStorage.key(index);
      if (key?.startsWith("ekiphan-zip-") && localStorage.getItem(key) === id) keys.push(key);
    }
    keys.forEach(key => localStorage.removeItem(key));
  } catch { /* Server state remains authoritative if browser storage is unavailable. */ }
}

class HttpError extends Error { constructor(public status: number, message: string, public code?: string) { super(message); } }
async function json<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${base}${path}`, { ...init, cache: "no-store" });
  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new HttpError(response.status, response.status === 401 ? "Yönetici oturumu sona erdi. Tekrar giriş yapıp aynı ZIP’i seçin; yüklenen parçalar korunuyor." : error.detail ?? `Yükleme isteği başarısız (HTTP ${response.status}).`, error.code);
  }
  return response.json() as Promise<T>;
}
async function retry<T>(work: () => Promise<T>, progress: (attempt: number) => void): Promise<T> {
  for (let attempt = 0; ; attempt++) {
    try { return await work(); }
    catch (error) {
      if (attempt >= 4 || (error instanceof HttpError && ![408, 429, 500, 502, 503, 504].includes(error.status))) throw error;
      progress(attempt + 1);
      await delay(Math.min(30000, 1000 * 2 ** attempt) + Math.random() * 500);
    }
  }
}
async function sha256(blob: Blob): Promise<string> {
  const hash = await crypto.subtle.digest("SHA-256", await blob.arrayBuffer());
  return Array.from(new Uint8Array(hash), byte => byte.toString(16).padStart(2, "0")).join("").toUpperCase();
}
function put(id: string, offset: number, body: Blob, hash: string, onProgress: (bytes: number) => void): Promise<void> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open("PUT", `${base}/${id}?offset=${offset}`);
    xhr.timeout = 15 * 60 * 1000;
    xhr.setRequestHeader("Content-Type", "application/octet-stream");
    xhr.setRequestHeader("X-Chunk-SHA256", hash);
    xhr.upload.onprogress = event => onProgress(event.loaded);
    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) resolve();
      else {
        let message = `Parça gönderilemedi (HTTP ${xhr.status}).`;
        try { message = JSON.parse(xhr.responseText).detail ?? message; } catch { /* proxy HTML */ }
        if (xhr.status === 401) message = "Yönetici oturumu sona erdi. Tekrar giriş yapıp aynı ZIP’i seçin; yüklenen parçalar korunuyor.";
        reject(new HttpError(xhr.status, message));
      }
    };
    xhr.onerror = xhr.ontimeout = () => reject(new Error("Bağlantı kesildi. Aynı dosyayı seçip kaldığı yerden sürdürebilirsiniz."));
    xhr.send(body);
  });
}

export async function uploadZip<T>(file: File, kind: UploadKind, onProgress: (value: ZipProgress) => void,
  onSession?: (id: string) => void, operation: "preview" | "execute" = "preview", fields?: Record<string, string>): Promise<{ id: string; preview: T }>  {
  const fingerprint = await sha256(new Blob([file.slice(0, fingerprintBytes), file.slice(-fingerprintBytes), `${file.size}:${file.lastModified}:${file.name}${fields ? JSON.stringify(Object.entries(fields).sort(([a], [b]) => a.localeCompare(b))) : ""}`]));
  const key = `ekiphan-zip-${kind}-${fingerprint}`;
  let state: State | undefined;
  let saved: string | null = null;
  try { saved = localStorage.getItem(key); } catch { /* private browser mode */ }
  if (saved) {
    try { state = await json<State>(`/${saved}?includeChunks=true`); }
    catch (error) { if (!(error instanceof HttpError) || error.status !== 404) throw error; }
  }
  if (state?.phase === "closed") {
    try { localStorage.removeItem(key); } catch { /* private browser mode */ }
    state = undefined;
  }
  if (!state) {
    // Creation is not blindly retried: a lost response must not reserve unlimited disk space.
    state = await json<State>("", { method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ kind, fileName: file.name, length: file.size, fingerprint }) });
    try { localStorage.setItem(key, state.id); } catch { /* resume unavailable across reload */ }
  }
  onSession?.(state.id);
  if (state.phase === "failed") throw new Error(state.error ?? "Önceki işlem başarısız; import geçmişini kontrol edin.");
  if (state.operation === "execute" && operation !== "execute") throw new Error(state.phase === "completed"
    ? "Bu ZIP'in içe aktarma işlemi tamamlandı. Mükerrer işlem başlatılmadı; import geçmişini kontrol edin."
    : "Bu ZIP sunucuda içe aktarılıyor. Mükerrer işlem başlatılmadı; import geçmişini kontrol edin.");
  // Verify every acknowledged prefix chunk before resuming, not only the sample fingerprint.
  for (const chunk of state.chunks) {
    if (await sha256(file.slice(chunk.offset, chunk.offset + chunk.length)) !== chunk.sha256)
      throw new Error("Seçilen dosya önceki yüklemeyle aynı değil. Farklı bir dosyayı bu yükleme üzerine yazamazsınız.");
  }
  for (let offset = state.offset; offset < file.size; offset += chunkBytes) {
    const part = file.slice(offset, offset + chunkBytes);
    const hash = await sha256(part);
    await retry(() => put(state!.id, offset, part, hash, bytes => onProgress({
      percent: Math.min(100, Math.floor((offset + bytes) * 100 / file.size)), phase: "transfer", message: "Dosya aktarılıyor" })),
      attempt => onProgress({ percent: Math.floor(offset * 100 / file.size), phase: "transfer", message: `Bağlantı yeniden deneniyor (${attempt}/4)` }));
  }
  const preview = await processZip<T>(state.id, operation, undefined, onProgress, undefined, fields);
  return { id: state.id, preview };
}

export async function processZip<T>(id: string, operation: "preview" | "validate" | "execute", titles: Record<string, string> | undefined,
  onProgress: (value: ZipProgress) => void, validationToken?: string, fields?: Record<string, string>): Promise<T> {
  onProgress({ percent: 100, phase: "processing", message: operation === "preview" ? "Aktarım tamamlandı; sunucuda ZIP doğrulanıyor" : "Sunucuda içe aktarılıyor" });
  let state = await retry(() => json<State>(`/${id}/process`, { method: "POST", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ operation, confirmed: operation === "execute", titles, validationToken, fields }) }), () => {});
  // Every HTTP request is short. Processing is independent of the browser connection.
  const deadline = Date.now() + 60 * 60 * 1000;
  while (state.phase === "processing" && Date.now() < deadline) {
    await delay(2500);
    state = await retry(() => json<State>(`/${id}`), () => {});
  }
  if (state.phase !== "completed") throw new Error(state.error ?? "İşlem sunucuda sürüyor. Aynı dosyayla devam ederek durumu kontrol edebilirsiniz.");
  onProgress({ percent: 100, phase: "ready", message: "Tamamlandı" });
  return state.result as T;
}

/** Same resumable transfer and idempotent worker as archive imports; existing single-file services persist the result. */
export async function uploadSingle<T>(file: File, kind: "image" | "pdf", fields: Record<string, string>,
  onProgress: (value: ZipProgress) => void, onSession?: (id: string) => void): Promise<T> {
  return (await uploadZip<T>(file, kind, onProgress, onSession, "execute", fields)).preview;
}
