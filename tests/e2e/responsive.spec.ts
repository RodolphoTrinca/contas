import { expect, test } from "@playwright/test";
import { login } from "./login";

test.use({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true });

test("mantém autenticação, cartões e formulários utilizáveis no celular", async ({ page }) => {
  const noHorizontalScroll = () => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth);
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Entrar no sistema" })).toBeVisible();
  expect(await noHorizontalScroll()).toBeTruthy();
  await page.getByRole("link", { name: "Entrar com Keycloak" }).click();
  await expect(page.locator('link[href$="/css/krt.css"]')).toHaveCount(1);
  await expect(page.getByRole("heading", { name: "Acesse sua conta de trabalho" })).toBeVisible();
  expect(await noHorizontalScroll()).toBeTruthy();
  await page.locator("#username").fill("operador");
  await page.locator("#password").fill("operador-local");
  await page.locator("#kc-login").click();
  await page.waitForURL(url => url.pathname === "/" && !url.search);

  await page.getByRole("main").getByRole("button", { name: "Nova conta" }).first().click();
  const drawer = page.getByRole("dialog", { name: "Abrir nova conta" });
  await expect(drawer).toBeVisible();
  expect(await drawer.evaluate(dialog => Math.abs(dialog.getBoundingClientRect().bottom - window.innerHeight))).toBeLessThan(2);
  await drawer.getByLabel("Nome do titular").fill("Titular com Nome Comprido para Visualização no Celular");
  await drawer.getByLabel("CPF", { exact: true }).fill("86291437112");
  await drawer.getByRole("button", { name: "Abrir conta" }).click();
  await expect(page.getByText("Conta aberta.")).toBeVisible();
  const row = page.getByRole("row").filter({ hasText: "Titular com Nome Comprido" });
  await expect(row).toBeVisible();
  expect(await row.evaluate(element => getComputedStyle(element).display)).toBe("grid");
  await page.setViewportSize({ width: 320, height: 720 });
  expect(await noHorizontalScroll()).toBeTruthy();
  await row.getByRole("button", { name: /editar conta/i }).click();
  const edit = page.getByRole("dialog", { name: "Editar conta" });
  await edit.getByRole("radio", { name: "Inativa", exact: true }).check();
  await edit.getByRole("button", { name: "Salvar alterações" }).click();
  await expect(row).toContainText("Inativa");
  await row.getByRole("button", { name: /excluir conta/i }).click();
  const deletion = page.getByRole("dialog", { name: /excluir a conta/i });
  await deletion.getByRole("button", { name: "Cancelar" }).click();
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: /excluir conta/i }).click();
  await deletion.getByRole("button", { name: "Confirmar exclusão" }).click();
  await expect(row).toHaveCount(0);
});

test("cliente não recebe controles de escrita no celular", async ({ page }) => {
  const [accounts] = await Promise.all([
    page.waitForResponse(response => response.url().endsWith("/backend/permissions")),
    login(page, "cliente"),
  ]);
  expect(accounts.ok()).toBeTruthy();
  await expect(page.getByRole("heading", { name: "Contas cadastradas" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Nova conta" })).toHaveCount(0);
  await expect(page.getByRole("button", { name: /editar conta|excluir conta/i })).toHaveCount(0);
});
