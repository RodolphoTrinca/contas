import { expect, test } from "@playwright/test";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { login } from "./login";

const history = process.env.ACCOUNTS_HISTORY_URL;
test.skip(!history, "Execute com ACCOUNTS_TEST_FANOUT=true para iniciar SNS, SQS e as demos.");

test("SNS entrega o mesmo evento a Fraude e Cartões e cada demo exibe seu processamento", async ({ page }) => {
  await login(page);
  const created = await page.request.post("/backend", {
    headers: { Origin: new URL(page.url()).origin, "Idempotency-Key": randomUUID() },
    data: { holderName: "Conta Fanout E2E", cpf: "93541134780" },
  });
  expect(created.status()).toBe(201);
  const id = (await created.json()).data.id;

  async function eventIds(consumer: string) {
    const response = await page.request.get(`${history}/${consumer}/messages`);
    if (!response.ok()) return [];
    const data = await response.json();
    return data.items.filter(item => item.accountId === id && item.processedAt).map(item => item.eventId);
  }

  await expect.poll(() => eventIds("fraud"), { timeout: 25_000 }).toHaveLength(1);
  await expect.poll(() => eventIds("cards"), { timeout: 25_000 }).toEqual(await eventIds("fraud"));

  expect((await page.request.get("/entregas")).status()).toBe(404);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.clock.install();
  await page.goto(history!);
  await expect(page.getByRole("banner").getByText("Banco KRT")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Histórico de entregas" })).toBeVisible();
  const events = page.getByRole("list", { name: "Mensagens" }).getByRole("listitem");
  await expect(events.filter({ hasText: id })).toContainText("Conta ativa sinalizada para análise");
  await page.getByRole("combobox", { name: "Aparência" }).selectOption("dark");
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await page.getByRole("combobox", { name: "Consumidor" }).selectOption("cards");
  await expect(events.filter({ hasText: id })).toContainText("Conta elegível para avaliação de cartões");
  const card = events.filter({ hasText: id });
  await card.getByText("Ver mensagem", { exact: true }).click();
  await expect(card.locator("pre")).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(390);
  await expect(card.locator("pre")).toContainText('"Type": "AccountCreated"');
  await expect(card.locator("pre")).toContainText(`"AccountId": "${id}"`);

  const refresh = page.waitForResponse(response => new URL(response.url()).pathname === "/cards/messages");
  await page.clock.fastForward(15_000);
  expect((await refresh).ok()).toBeTruthy();
  await expect(card.locator("details")).toHaveAttribute("open", "");
  await expect(page.getByRole("combobox", { name: "Consumidor" })).toHaveValue("cards");

  const project = process.env.ACCOUNTS_FANOUT_PROJECT;
  if (!project) return;
  const [eventId] = await eventIds("fraud");
  const payload = JSON.stringify({
    EventId: eventId, AccountId: id, Type: "AccountCreated", Version: 1, Status: 0,
    OccurredAt: new Date().toISOString()
  });
  const aws = (...args: string[]) =>
    execFileSync("docker", ["exec", `${project}-floci-1`, "aws", "--endpoint-url", "http://127.0.0.1:4566",
      ...args], { encoding: "utf8", timeout: 5000 });
  aws("sns", "publish", "--topic-arn", "arn:aws:sns:us-east-1:000000000000:account-events", "--message", payload);
  for (const consumer of ["fraud", "cards"]) {
    await expect.poll(() => {
      const counts = JSON.parse(aws("sqs", "get-queue-attributes", "--queue-url",
        `http://floci:4566/000000000000/${consumer}-events`, "--attribute-names",
        "ApproximateNumberOfMessages", "ApproximateNumberOfMessagesNotVisible")).Attributes;
      return Number(counts.ApproximateNumberOfMessages) + Number(counts.ApproximateNumberOfMessagesNotVisible);
    }, { timeout: 15_000 }).toBe(0);
    expect(await eventIds(consumer)).toEqual([eventId]);
  }

});
