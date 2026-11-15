import { expect, test, type Page } from "@playwright/test";

const appearance = (page: Page) => page.getByRole("combobox", { name: "Aparência", exact: true });
const theme = (page: Page, value: string) => expect(page.locator("html")).toHaveAttribute("data-theme", value);

test("aparência segue o sistema, persiste a escolha e sincroniza entre abas", async ({ page, context }) => {
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  page.on("console", message => {
    if (message.type() === "error" && /hydration|hydrated/i.test(message.text())) errors.push(message.text());
  });
  await page.emulateMedia({ colorScheme: "dark" });
  await page.goto("/");
  await theme(page, "dark");
  await expect(appearance(page)).toHaveValue("system");
  await page.emulateMedia({ colorScheme: "light" });
  await theme(page, "light");
  await appearance(page).selectOption("dark");
  await theme(page, "dark");
  await page.reload();
  await theme(page, "dark");
  await expect(appearance(page)).toHaveValue("dark");

  const other = await context.newPage();
  await other.emulateMedia({ colorScheme: "light" });
  await other.goto("/");
  await theme(other, "dark");
  await appearance(page).selectOption("light");
  await theme(other, "light");
  await expect(appearance(other)).toHaveValue("light");
  await page.emulateMedia({ colorScheme: "dark" });
  await theme(page, "light");
  await appearance(page).selectOption("system");
  await theme(page, "dark");
  await theme(other, "light");
  await expect(appearance(other)).toHaveValue("system");
  expect(errors).toEqual([]);
});

test("preferência inválida e armazenamento indisponível não impedem trocar aparência", async ({ page, browser }) => {
  await page.emulateMedia({ colorScheme: "dark" });
  await page.addInitScript(() => localStorage.setItem("krt-appearance", "invalid"));
  await page.goto("/");
  await theme(page, "dark");
  await expect(appearance(page)).toHaveValue("system");
  const blocked = await browser.newPage({ colorScheme: "dark" });
  await blocked.addInitScript(() => {
    Object.defineProperty(window, "localStorage", { get() { throw new DOMException("Storage disabled", "SecurityError"); } });
  });
  await blocked.goto("/");
  await theme(blocked, "dark");
  await appearance(blocked).selectOption("light");
  await theme(blocked, "light");
  await expect(blocked.getByRole("link", { name: "Fazer login" })).toBeVisible();
  await blocked.close();
});

test("preferência acompanha login, erro e saída do Keycloak no celular", async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 320, height: 720 });
  await page.emulateMedia({ colorScheme: "light" });
  await page.goto("/");
  await appearance(page).selectOption("dark");
  await page.getByRole("link", { name: "Fazer login" }).click();
  await theme(page, "dark");
  await expect(appearance(page)).toHaveValue("dark");
  await page.locator("#username").fill("operador");
  await page.locator("#password").fill("senha-incorreta");
  await page.locator("#kc-login").click();
  await expect(page.locator("#input-error-username")).toBeVisible();
  await theme(page, "dark");
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await testInfo.attach("keycloak-dark-error", { body: await page.screenshot({ fullPage: true }), contentType: "image/png" });
  await page.locator("#password").fill("operador-local");
  await page.locator("#kc-login").click();
  await expect(page.getByRole("heading", { name: "Contas", exact: true })).toBeVisible();
  await theme(page, "dark");
  await expect(appearance(page)).toHaveValue("dark");
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.getByRole("button", { name: "Sair", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Confirmar saída", exact: true })).toBeVisible();
  await theme(page, "dark");
  await testInfo.attach("keycloak-dark-logout", { body: await page.screenshot({ fullPage: true }), contentType: "image/png" });
  await appearance(page).selectOption("light");
  await theme(page, "light");
  await page.getByRole("button", { name: "Confirmar saída", exact: true }).click();
  await expect(page.getByRole("link", { name: "Fazer login" })).toBeVisible();
  await theme(page, "light");
  await expect(appearance(page)).toHaveValue("light");
});
