import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: ".",
  use: { baseURL: process.env.ACCOUNTS_E2E_URL ?? "http://127.0.0.1:8080" },
});
