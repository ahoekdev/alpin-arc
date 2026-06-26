import { defineConfig } from "orval";
import nextEnv from "@next/env";

const { loadEnvConfig } = nextEnv;

loadEnvConfig(process.cwd());

const apiBaseUrl = process.env.API_BASE_URL;

if (!apiBaseUrl) {
  throw new Error("API_BASE_URL is not defined in environment variables");
}

export default defineConfig({
  alpinarc: {
    input: {
      target: `${apiBaseUrl}/openapi/v1.json`,
    },
    output: {
      target: "lib/api/generated/orval/client.ts",
      schemas: {
        path: "lib/api/generated/orval/model",
        type: "typescript",
      },
      client: "fetch",
      mode: "tags-split",
      baseUrl: {
        runtime: "getApiBaseUrl()",
        imports: [
          {
            name: "getApiBaseUrl",
            default: true,
            importPath: "../../../utils/getApiBaseUrl",
          },
        ],
      },
      override: {
        useTypeOverInterfaces: true,
      },
    },
  },
});
