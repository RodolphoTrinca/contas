import { test, expect } from "@playwright/test";
import { login } from "./login";

test("login OIDC cria sessão HttpOnly e logout encerra o acesso", async ({ page, request }) => {
  const anonymous = await request.get("/backend");
  expect(anonymous.status()).toBe(401);
  await login(page);
  const logout = page.getByRole("button", { name: "Sair", exact: true });
  await logout.click();
  const confirmation = page.getByRole("button", { name: /confirmar saída|logout|sign out/i });
  await confirmation.click();
  await expect(page.getByRole("link", { name: "Entrar com Keycloak" })).toBeVisible();
  expect((await page.context().cookies()).some(cookie => cookie.name === "contas_access")).toBe(false);
});
