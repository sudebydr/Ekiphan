"use client";

import { useEffect, useState } from "react";
import { closeZipUpload, listZipUploads, type UploadKind, type ZipUploadSession } from "../../../lib/resumable-zip-upload";

const phaseLabel = (session: ZipUploadSession) => session.phase === "uploading" ? "Devam ettirilebilir aktarım"
  : session.phase === "processing" ? "Sunucuda işleniyor"
  : session.phase === "failed" ? "Başarısız — import geçmişini kontrol edin"
  : session.phase === "closed" ? "Kapatıldı — dosya temizliği bekliyor"
  : (session.operation === "preview" || session.operation === "validate") ? "Önizleme hazır — onay bekliyor" : "İçe aktarma tamamlandı";

export function ZipUploadSessions({ kind, refreshKey, disabled, onClosed }: {
  kind: UploadKind; refreshKey: string; disabled: boolean; onClosed: (id: string) => void;
}) {
  const [sessions, setSessions] = useState<ZipUploadSession[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [closing, setClosing] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let current = true;
    listZipUploads().then(items => { if (current) { setSessions(items.filter(item => item.kind === kind)); setError(null); } })
      .catch(reason => { if (current) setError(reason instanceof Error ? reason.message : "Yüklemeler listelenemedi."); });
    return () => { current = false; };
  }, [kind, refreshKey, revision]);

  async function close(session: ZipUploadSession) {
    if (!window.confirm(`${session.fileName} yüklemesi kapatılsın mı? Bekleyen önizleme/aktarım artık devam ettirilemez. İçe aktarılmış ürün, katalog ve medya kayıtları silinmez.`)) return;
    setClosing(session.id); setError(null);
    try {
      await closeZipUpload(session.id);
      setSessions(items => items.filter(item => item.id !== session.id));
      onClosed(session.id);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Yükleme kapatılamadı."); }
    finally { setClosing(null); setRevision(value => value + 1); }
  }

  return <details>
    <summary>Geçici yüklemelerim ({sessions.length})</summary>
    <p>Farklı ZIP seçmek eski aktarımı veya onay bekleyen önizlemeyi silmez. Aynı dosyayı seçerek devam edebilir, kullanmayacağınız yüklemeyi açıkça kapatabilirsiniz.</p>
    <p>Oturum sayısı sınırı yoktur; disk kotası korunur. Onay bekleyen önizlemeler siz kapatana kadar saklanır. Yarım aktarımlar son etkinlikten 7 gün sonra temizlenebilir.</p>
    <button type="button" disabled={disabled || closing !== null} onClick={() => setRevision(value => value + 1)}>Listeyi yenile</button>
    {error && <p role="alert">{error}</p>}
    <ul>{sessions.map(session => <li key={session.id}>
      <strong style={{ overflowWrap: "anywhere" }}>{session.fileName}</strong> — {phaseLabel(session)} · {(session.length / 1024 / 1024).toFixed(1)} MB
      {" "}<button type="button" disabled={disabled || closing !== null || !session.canClose} onClick={() => void close(session)}>
        {closing === session.id ? "Kapatılıyor…" : "Yüklemeyi kapat / Temizle"}
      </button>
    </li>)}</ul>
    {sessions.length === 0 && <p>Temizlenecek geçici ZIP yüklemesi yok.</p>}
  </details>;
}
