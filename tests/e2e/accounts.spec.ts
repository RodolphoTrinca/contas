import { test, expect } from "@playwright/test";
import { login } from "./login";

test("abre, reenvia após perda da resposta, altera e exclui pelo Nginx", async ({ page }) => {
  const keys: string[] = [];
  const statuses: number[] = [];
  const replays: (string | undefined)[] = [];
  await page.route(/\/backend\/?$/, async route => {
    if (route.request().method() !== "POST") return route.continue();
    keys.push(route.request().headers()["idempotency-key"]);
    const response = await route.fetch();
    statuses.push(response.status());
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
  await page.getByLabel("Status").selectOption("inactive");
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
