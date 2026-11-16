import { expect, test } from "@playwright/test";
import { isDefinitiveStatus, prepareWrite } from "../../src/Contas.Web/features/accounts/pending-write";

test("reenvio após recarga mantém chave e versão originais", async () => {
  const data = new Map<string, string>();
  const storage = () => ({
    getItem: (key: string) => data.get(key) ?? null,
    setItem: (key: string, value: string) => { data.set(key, value); },
    removeItem: (key: string) => { data.delete(key); },
  });
  const body = JSON.stringify({ holderName: "Conta Teste", cpf: "52998224725" });
  const first = await prepareWrite(storage(), "operator-a", "PUT", "/backend/account", body, 1);
  expect(first).not.toBeNull();
  expect(await prepareWrite(storage(), "operator-a", "PUT", "/backend/account", body, 2)).toEqual(first);
  expect((await prepareWrite(storage(), "operator-b", "PUT", "/backend/account", body, 2))?.key).not.toBe(first!.key);
  expect(JSON.stringify([...data])).not.toContain("52998224725");

  storage().removeItem(first!.storageKey);
  expect((await prepareWrite(storage(), "operator-a", "PUT", "/backend/account", body, 2))?.key).not.toBe(first!.key);
});

test("tentativa expirada não gera outra chave e falhas ambíguas preservam a chave", async () => {
  const data = new Map<string, string>();
  const storage = {
    getItem: (key: string) => data.get(key) ?? null,
    setItem: (key: string, value: string) => { data.set(key, value); },
    removeItem: (key: string) => { data.delete(key); },
  };
  await prepareWrite(storage, "operator", "DELETE", "/backend/account", undefined, 1, 0);
  expect(await prepareWrite(storage, "operator", "DELETE", "/backend/account", undefined, 2, 86_400_000)).toBeNull();
  expect(data.size).toBe(1);
  for (const status of [401, 408, 429, 500, 502, 503, 504]) expect(isDefinitiveStatus(status)).toBe(false);
  for (const status of [200, 201, 400, 409]) expect(isDefinitiveStatus(status)).toBe(true);
});
