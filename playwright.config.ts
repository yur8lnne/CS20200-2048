import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "tests/e2e",
  timeout: 30_000,
  expect: {
    timeout: 5_000
  },
  use: {
    baseURL: "http://127.0.0.1:5081",
    trace: "retain-on-failure"
  },
  webServer: {
    command: "dotnet run --project src/Server --urls http://127.0.0.1:5081",
    url: "http://127.0.0.1:5081/health",
    timeout: 60_000,
    reuseExistingServer: !process.env.CI,
    env: {
      DB_PATH: "data/e2e-leaderboard.sqlite"
    }
  },
  projects: [
    {
      name: "chromium-desktop",
      use: {
        ...devices["Desktop Chrome"],
        viewport: { width: 1440, height: 900 }
      }
    },
    {
      name: "chromium-tablet",
      use: {
        ...devices["Desktop Chrome"],
        viewport: { width: 768, height: 900 }
      }
    },
    {
      name: "chromium-mobile",
      use: {
        ...devices["Pixel 5"]
      }
    }
  ]
});
