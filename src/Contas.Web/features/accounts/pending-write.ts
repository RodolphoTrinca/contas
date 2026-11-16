const retentionMs = 24 * 60 * 60 * 1000;

type PendingWrite = { key: string; expectedVersion?: number; startedAt: number };
type WriteStorage = Pick<Storage, "getItem" | "setItem" | "removeItem">;

export async function prepareWrite(storage: WriteStorage, scope: string, method: string, url: string,
  content?: string, expectedVersion?: number, now = Date.now()) {
  const input = new TextEncoder().encode(JSON.stringify([method, url, content]));
  const hash = await crypto.subtle.digest("SHA-256", input);
  const fingerprint = Array.from(new Uint8Array(hash), byte => byte.toString(16).padStart(2, "0")).join("");
  const storageKey = `contas:pending-write:${scope}:${fingerprint}`;
  const saved = storage.getItem(storageKey);
  const attempt: PendingWrite = saved === null
    ? { key: crypto.randomUUID(), expectedVersion, startedAt: now }
    : JSON.parse(saved);
  if (now - attempt.startedAt >= retentionMs) return null;

  if (saved === null) storage.setItem(storageKey, JSON.stringify(attempt));
  return { ...attempt, storageKey };
}

export function isDefinitiveStatus(status: number) {
  return status < 500 && ![401, 408, 429].includes(status);
}
