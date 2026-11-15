import { expect, type Page } from "@playwright/test";

export async function login(page: Page, username = "operador") {
  await page.goto("/");
  await page.getByRole("link", { name: "Fazer login" }).click();
  await page.locator("#username").fill(username);
  await page.locator("#password").fill(username + "-local");
  await page.locator("#kc-login").click();
  await expect(page).toHaveURL(url => url.pathname === "/" && url.search === "");
  const cookie = (await page.context().cookies()).find(item => item.name === "contas_access");
  expect(cookie?.httpOnly).toBe(true);
}
