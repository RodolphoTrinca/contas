import { test, expect } from "@playwright/test";
import { login } from "./login";

test("abre, reenvia após perda da resposta, altera e exclui pelo Nginx", async ({ page }) => {
  let accountId = "";
  const keys: string[] = [];
  const statuses: number[] = [];
  const replays: (string | undefined)[] = [];
  await page.route(/\/backend\/?$/, async route => {
    if (route.request().method() !== "POST") return route.continue();
    keys.push(route.request().headers()["idempotency-key"]);
    const response = await route.fetch();
    statuses.push(response.status());
    if (response.ok()) accountId = (await response.json()).data.id;
    replays.push(response.headers()["idempotency-replayed"]);
    if (keys.length === 1) await route.abort("connectionreset");
    else await route.fulfill({ response });
  });

  await login(page);
  await expect(page.getByRole("heading", { name: "Contas", exact: true })).toBeVisible();
  await page.getByRole("main").getByRole("button", { name: "Nova conta" }).first().click();
  await page.getByLabel("Nome do titular").fill("Ana Teste E2E");
  await page.getByLabel("CPF", { exact: true }).fill("529.982.247-25");
  const open = page.getByRole("button", { name: "Abrir conta", exact: true });
  await open.click();
  await expect(page.getByRole("alert").filter({ hasText: "A conexão falhou" })).toBeVisible();
  await open.click();
  await expect(page.getByText("Conta aberta.")).toBeVisible();
  expect(keys).toHaveLength(2);
  expect(keys[0]).toMatch(/^[0-9a-f-]{36}$/);
  expect(keys[1]).toBe(keys[0]);
  expect(statuses).toEqual([201, 201]);
  expect(replays).toEqual([undefined, "true"]);

  const row = page.getByRole("row").filter({ hasText: "Ana Teste E2E" });
  await expect(row).toHaveCount(1);
  await expect(row).toContainText("***.***.***-25");
  await row.getByRole("button", { name: /editar conta/i }).click();
  await page.getByLabel("Nome do titular").fill("Ana Atualizada E2E");
  await page.getByRole("radio", { name: "Inativa", exact: true }).check();
  await page.getByRole("button", { name: "Salvar alterações" }).click();
  const updated = page.getByRole("row").filter({ hasText: "Ana Atualizada E2E" });
  await expect(updated).toContainText("Inativa");
  await updated.getByRole("button", { name: /excluir conta/i }).click();
  await page.getByRole("button", { name: "Confirmar exclusão" }).click();
  await expect(page.getByText("Conta excluída.")).toBeVisible();
  await expect(updated).toHaveCount(0);
  await page.reload();
  await expect(page.getByRole("heading", { name: "Contas cadastradas" })).toBeVisible();
  await expect(updated).toHaveCount(0);

  await page.setViewportSize({ width: 320, height: 720 });
  await page.getByRole("link", { name: "Histórico de alterações" }).click();
  await page.getByLabel("ID da conta").fill(accountId);
  await page.getByRole("button", { name: "Filtrar", exact: true }).click();
  const history = page.getByRole("list", { name: "Alterações" }).getByRole("listitem");
  await expect(history).toHaveCount(3);
  await expect(history.first()).toContainText("Exclusão de conta");
  await expect(history.nth(1)).toContainText("Edição de conta");
  await expect(history.last()).toContainText("Abertura de conta");
  for (const item of await history.all()) {
    await expect(item).toContainText(accountId);
    await expect(item).toContainText(/contas-web:[0-9a-f-]{36}/i);
  }
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBe(320);
});

test("publica Scalar e o contrato OpenAPI pelo Nginx", async ({ page, request }) => {
  const response = await request.get("/openapi/v1.json");
  expect(response.ok()).toBeTruthy();
  expect(response.headers().server).toContain("nginx");
  const contract = await response.json();
  expect(contract.paths["/api/v1/accounts"].post.parameters).toEqual(
    expect.arrayContaining([expect.objectContaining({ name: "Idempotency-Key", in: "header", required: true })]),
  );
  const scalar = await page.goto("/scalar/v1");
  expect(scalar?.ok()).toBeTruthy();
  await expect(page).toHaveTitle(/scalar|api reference/i);
});


test("restringe o histórico ao Operador", async ({ page }) => {
  expect((await page.request.get("/backend/history")).status()).toBe(401);
  await login(page, "gerente-a");
  await expect(page.getByRole("heading", { name: "Contas cadastradas" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Histórico de alterações" })).toHaveCount(0);
  expect((await page.request.get("/backend/history")).status()).toBe(403);
  await page.goto("/historico");
  await expect(page.getByRole("main").getByRole("alert")).toContainText("apenas para o Operador");
  await expect(page.getByRole("list", { name: "Alterações" })).toHaveCount(0);
});
