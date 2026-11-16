import { expect, test, type Browser } from "@playwright/test";
import { randomUUID } from "node:crypto";
import { login } from "./login";

test("gerente opera só na carteira, cliente só consulta e operador transfere", async ({ browser, request }) => {
  async function identity(browser: Browser, username: string) {
    const context = await browser.newContext();
    try {
      const page = await context.newPage();
      await login(page, username);
      return "Bearer " + (await context.cookies()).find(cookie => cookie.name === "contas_access")!.value;
    } finally {
      await context.close();
    }
  }
  const [a, b, customer, operator] = await Promise.all(
    ["gerente-a", "gerente-b", "cliente", "operador"].map(user => identity(browser, user)));
  const base = "/api/v1/accounts";
  const headers = (token: string, key = randomUUID()) => ({ Authorization: token, "Idempotency-Key": key });
  const key = randomUUID();
  const data = { holderName: "Conta Carteira E2E", cpf: "11144477735" };
  const created = await request.post(base, { headers: headers(a, key), data });
  expect(created.status()).toBe(201);
  const { data: { id, version } } = await created.json();
  const path = base + "/" + id;
  const read = (path: string, token: string) => request.get(path, { headers: headers(token) });
  let expectedVersion = version;
  const writeHeaders = (token: string) => ({ ...headers(token), "Expected-Version": String(expectedVersion) });
  const revise = (token: string, status: string) => request.put(path, { headers: writeHeaders(token), data: { holderName: data.holderName, status } });
  const assign = (token: string, kind: string, subject: string) => request.put(path + "/" + kind, { headers: headers(token), data: { subject } });
  const managerSubject = "74113d8a-7870-4e36-a8f8-141a69a8a602";
  const ownerSubject = "74113d8a-7870-4e36-a8f8-141a69a8a603";

  expect((await read(path, operator)).status()).toBe(200);
  expect((await read(path, a)).status()).toBe(200);
  expect((await read(path, b)).status()).toBe(404);
  expect((await revise(b, "inactive")).status()).toBe(404);
  expect((await request.delete(path, { headers: writeHeaders(b) })).status()).toBe(404);
  expect((await assign(a, "portfolio", managerSubject)).status()).toBe(403);
  const editHeaders = writeHeaders(a);
  const editData = { holderName: data.holderName, status: "inactive" };
  expect((await request.put(path, { headers: editHeaders, data: editData })).status()).toBe(200);
  const replay = await request.put(path, { headers: editHeaders, data: editData });
  expect(replay.status()).toBe(200);
  expect(replay.headers()["idempotency-replayed"]).toBe("true");
  expect((await revise(a, "active")).status()).toBe(409);
  expectedVersion++;
  expect((await revise(a, "active")).status()).toBe(200);
  expectedVersion++;

  expect((await read(path, customer)).status()).toBe(404);
  expect((await assign(operator, "owner", ownerSubject)).status()).toBe(200);
  expect((await read(path, customer)).status()).toBe(200);
  expect((await (await read(base, customer)).json()).data.map((row: { id: string }) => row.id)).toEqual([id]);
  expect((await revise(customer, "inactive")).status()).toBe(403);

  expect((await assign(operator, "portfolio", managerSubject)).status()).toBe(200);
  expect((await read(path, a)).status()).toBe(404);
  expect((await (await read(base, a)).json()).data).toEqual([]);
  expect((await (await read(base, b)).json()).data.map((row: { id: string }) => row.id)).toEqual([id]);
  expect((await request.post(base, { headers: headers(a, key), data })).status()).toBe(404);
  expect((await request.delete(path, { headers: writeHeaders(b) })).status()).toBe(200);
  expect((await read(path, customer)).status()).toBe(404);
});
