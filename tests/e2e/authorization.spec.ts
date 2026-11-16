import { test, expect } from "@playwright/test";
import { login } from "./login";
import { randomUUID } from "node:crypto";

test("clientes de serviço só acessam a rota de sua role", async ({ request, page }) => {
  const anonymous = await request.get("/api/v1/accounts");
  expect(anonymous.status()).toBe(401);
  expect(await anonymous.json()).toMatchObject({ data: null, error: { code: "auth.required" } });

  await login(page);
  const operator = (await page.context().cookies()).find(cookie => cookie.name === "contas_access")!.value;
  const headers = { Authorization: "Bearer " + operator, "Idempotency-Key": randomUUID() };
  const invalid = await request.post("/api/v1/accounts", { headers, data: { holderName: "Teste", cpf: "000" } });
  expect(invalid.status()).toBe(400);
  expect(await invalid.json()).toMatchObject({ data: null, error: { code: "account.invalid_cpf" } });
  const created = await request.post("/api/v1/accounts", {
    headers, data: { holderName: "Conta de autorização", cpf: "12345678909" },
  });
  expect(created.status()).toBe(201);
  const { data: account } = await created.json();
  const fraud = "/api/v1/fraud/accounts/" + account.id + "/status";
  const cards = "/api/v1/cards/accounts/" + account.id + "/eligibility";

  for (const [id, own, other] of [["fraud-api", fraud, cards], ["cards-api", cards, fraud]]) {
    const token = await request.post("/auth/realms/contas/protocol/openid-connect/token", {
      form: { grant_type: "client_credentials", client_id: id, client_secret: id + "-local" },
    });
    expect(token.ok()).toBeTruthy();
    const serviceHeaders = { Authorization: "Bearer " + (await token.json()).access_token };
    const allowed = await request.get(own, { headers: serviceHeaders });
    expect(allowed.status()).toBe(200);
    expect(await allowed.json()).toMatchObject({ data: { id: account.id }, error: null });
    for (const path of [other, "/api/v1/accounts"]) {
      const denied = await request.get(path, { headers: serviceHeaders });
      expect(denied.status()).toBe(403);
      expect(await denied.json()).toMatchObject({ data: null, error: { code: "auth.forbidden" } });
    }
  }
  expect((await request.get(fraud, { headers })).status()).toBe(403);
  expect((await request.get(cards, { headers })).status()).toBe(403);
  const deleted = await request.delete("/api/v1/accounts/" + account.id, {
    headers: { ...headers, "Idempotency-Key": randomUUID(), "Expected-Version": String(account.version) },
  });
  expect(deleted.status()).toBe(200);
  expect(await deleted.json()).toMatchObject({ data: { id: account.id, deleted: true }, error: null });
});
