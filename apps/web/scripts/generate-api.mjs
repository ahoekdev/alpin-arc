import { spawnSync } from "node:child_process";

import nextEnv from "@next/env";

const { loadEnvConfig } = nextEnv;

loadEnvConfig(process.cwd());

const apiBaseUrl = process.env.API_BASE_URL;

if (!apiBaseUrl) {
  throw new Error("API_BASE_URL is not defined in environment variables");
}

const openApiUrl = `${apiBaseUrl}/openapi/v1.json`;

const result = spawnSync(
  "openapi-typescript",
  [openApiUrl, "-o", "lib/api/generated/schema.d.ts"],
  {
    stdio: "inherit",
    shell: process.platform === "win32",
  },
);

process.exit(result.status ?? 1);
