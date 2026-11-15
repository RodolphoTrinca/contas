import { expect, test, type Page } from "@playwright/test";

const accounts = [
  { id: "b8e7303e-282b-4495-81c6-998148f60101", holderName: "Mariana Duarte Peixoto", cpf: "***.***.***-42", status: "active", version: 1 },
  { id: "b8e7303e-282b-4495-81c6-998148f60102", holderName: "Alexandre de Albuquerque Vasconcelos Silveira Braga", cpf: "***.***.***-99", status: "inactive", version: 1 },
];

async function checkViewport(page: Page) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const dialog = page.getByRole("dialog");
  if (await dialog.count()) {
    const box = await dialog.boundingBox();
    expect(box!.x).toBeGreaterThanOrEqual(0);
    expect(box!.x + box!.width).toBeLessThanOrEqual(page.viewportSize()!.width + 1);
    expect(box!.y).toBeGreaterThanOrEqual(0);
    expect(box!.y + box!.height).toBeLessThanOrEqual(page.viewportSize()!.height + 1);
    expect(await dialog.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
  }
}

for (const colorScheme of ["light", "dark"] as const) {
  test.describe(`aparência ${colorScheme}`, () => {
    test.use({ colorScheme });
    for (const width of [1440, 768, 390, 320]) {
      test(`design v2: telas, filtros e foco em ${width}px`, async ({ page }, testInfo) => {
        await page.setViewportSize({ width, height: width < 768 ? 844 : 960 });
        let state: "ready" | "empty" | "forbidden" | "unavailable" | "expired" = "ready";
        await page.route(/\/backend(?:\/permissions|\?.*)?$/, async route => {
          if (route.request().url().endsWith("/permissions")) return route.fulfill({ json: { data: { canWrite: true, canAssign: true } } });
          return route.fulfill({ status: state === "forbidden" ? 403 : state === "unavailable" ? 503 : state === "expired" ? 401 : 200, json: { data: state === "empty" ? [] : accounts } });
        });
        const capture = async (name: string) => {
          await checkViewport(page);
          await testInfo.attach(`${width}-${name}`, { body: await page.screenshot({ fullPage: !(await page.getByRole("dialog").count()), animations: "disabled" }), contentType: "image/png" });
        };
        await page.goto("/");
        await expect(page.locator("html")).toHaveAttribute("data-theme", colorScheme);
        await expect(page.getByRole("row").filter({ hasText: accounts[1].holderName })).toBeVisible();
        await page.evaluate(() => document.fonts.ready);
        await capture("contas");
        await page.getByRole("button", { name: "Inativas", exact: false }).click();
        await expect(page.getByRole("row").filter({ hasText: accounts[0].holderName })).toHaveCount(0);
        await page.getByRole("button", { name: "Todas", exact: false }).click();
        await page.getByRole("searchbox").fill("Mariana");
        await expect(page.getByRole("row").filter({ hasText: accounts[1].holderName })).toHaveCount(0);
        await page.getByRole("button", { name: "Limpar busca" }).click();
        await page.getByRole("button", { name: "Nova conta", exact: true }).click();
        let dialog = page.getByRole("dialog", { name: "Abrir nova conta" });
        await expect(dialog.getByLabel("Nome do titular")).toBeFocused();
        await dialog.getByLabel("Nome do titular").fill("Mariana Duarte Peixoto");
        await dialog.getByLabel("CPF", { exact: true }).fill("52998224725");
        await expect(dialog.getByLabel("CPF", { exact: true })).toHaveValue("529.982.247-25");
        await capture("abertura");
        await dialog.getByLabel("CPF", { exact: true }).fill("11111111111");
        await dialog.getByRole("button", { name: "Abrir conta", exact: true }).click();
        await expect(dialog.getByRole("alert")).toContainText("11 dígitos válidos");
        await capture("cpf-invalido");
        await page.keyboard.press("Escape");
        await expect(page.getByRole("button", { name: "Nova conta", exact: true })).toBeFocused();
        const row = page.getByRole("row").filter({ hasText: accounts[1].holderName });
        await row.getByRole("button", { name: /Editar conta/ }).click();
        dialog = page.getByRole("dialog", { name: "Editar conta", exact: true });
        await expect(dialog.getByLabel("CPF", { exact: true })).toHaveAttribute("readonly", "");
        await dialog.getByRole("radio", { name: "Ativa", exact: true }).check();
        await expect(dialog.getByRole("radio", { name: "Ativa", exact: true })).toBeChecked();
        await capture("edicao");
        await page.keyboard.press("Escape");
        for (const operation of ["Transferir carteira", "Vincular titular"]) {
          await row.getByRole("button", { name: operation }).click();
          await expect(page.getByRole("dialog", { name: operation })).toBeVisible();
          await capture(operation === "Transferir carteira" ? "carteira" : "titular");
          await page.keyboard.press("Escape");
        }
        await row.getByRole("button", { name: /Excluir conta/ }).click();
        dialog = page.getByRole("dialog");
        await expect(dialog.getByRole("button", { name: "Cancelar", exact: true })).toBeFocused();
        await capture("exclusao");
        await dialog.getByRole("button", { name: "Cancelar", exact: true }).click();
        await expect(row.getByRole("button", { name: /Excluir conta/ })).toBeFocused();
        state = "empty";
        await page.getByRole("button", { name: "Atualizar lista" }).click();
        await expect(page.getByText("Nenhuma conta nesta página.")).toBeVisible();
        await capture("vazio");
        state = "unavailable";
        await page.getByRole("button", { name: "Atualizar lista" }).click();
        await expect(page.getByRole("main").getByRole("alert")).toContainText("Não foi possível atualizar");
        await capture("erro");
        state = "expired";
        await page.getByRole("button", { name: "Atualizar lista" }).click();
        await expect(page.getByRole("main").getByRole("alert")).toContainText("Sua sessão expirou");
        await capture("sessao-expirada");
        state = "forbidden";
        await page.reload();
        await expect(page.getByRole("main").getByRole("alert")).toContainText("Acesso não autorizado");
        await expect(page.getByRole("button", { name: "Nova conta" })).toHaveCount(0);
        await capture("sem-permissao");
      });
    }
  });
}

test("tema v2 do Keycloak mantém erro de login, visibilidade da senha e saída no celular", async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Entrar no sistema" })).toBeVisible();
  await testInfo.attach("entrada-mobile", { body: await page.screenshot({ fullPage: true }), contentType: "image/png" });
  await page.getByRole("link", { name: "Fazer login" }).click();
  await expect(page.locator(".krt-auth-brand")).toBeVisible();
  await checkViewport(page);
  await page.locator("#username").fill("operador");
  await page.locator("#password").fill("senha-incorreta");
  await page.locator("#kc-login").click();
  await expect(page.locator("#input-error-username")).toBeVisible();
  await testInfo.attach("keycloak-erro-mobile", { body: await page.screenshot({ fullPage: true }), contentType: "image/png" });
  await page.locator("#password").fill("operador-local");
  await page.getByRole("button", { name: /exibir senha|mostrar senha|show password/i }).click();
  await expect(page.locator("#password")).toHaveAttribute("type", "text");
  await page.locator("#kc-login").click();
  await expect(page.getByRole("heading", { name: "Contas", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Sair", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Confirmar saída", exact: true })).toBeVisible();
  await checkViewport(page);
  await testInfo.attach("keycloak-saida-mobile", { body: await page.screenshot({ fullPage: true }), contentType: "image/png" });
  await page.getByRole("button", { name: "Confirmar saída", exact: true }).click();
  await expect(page.getByRole("link", { name: "Fazer login" })).toBeVisible();
});
