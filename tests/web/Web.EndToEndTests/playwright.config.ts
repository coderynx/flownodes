import { defineConfig, devices } from "@playwright/test";

export const hostUrl = "http://localhost:5199";

export default defineConfig({
  testDir: "./tests",
  globalSetup: "./global-setup.ts",
  forbidOnly: !!process.env.CI,
  workers: 1,
  reporter: process.env.CI ? "github" : "list",
  use: {
    baseURL: hostUrl,
    trace: "retain-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
