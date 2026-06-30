import { ensureCsrfToken } from "./csrf";

const mutatingMethods = new Set(["POST", "PUT", "PATCH", "DELETE"]);

type ApiError = {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  info?: unknown;
};

export async function apiFetch<T>(
  url: string,
  options: RequestInit = {},
): Promise<T> {
  const method = (options.method ?? "GET").toUpperCase();
  const headers = new Headers(options.headers);

  if (typeof window !== "undefined" && mutatingMethods.has(method)) {
    const token = await ensureCsrfToken();
    if (token) {
      headers.set("X-XSRF-TOKEN", token);
    }
  }

  if (options.body !== undefined && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(url, {
    ...options,
    headers,
    credentials: "same-origin",
  });

  const contentType = (response.headers.get("content-type") ?? "").toLowerCase();
  const body = [204, 205, 304].includes(response.status) ? null : await response.text();

  if (!response.ok) {
    const error: ApiError = {
      status: response.status,
      info: parseBody(body, contentType),
    };

    throw error;
  }

  return {
    data: parseBody(body, contentType),
    status: response.status,
    headers: response.headers,
  } as T;
}

function parseBody(body: string | null, contentType: string): unknown {
  if (body === null) {
    return undefined;
  }

  if (contentType.includes("json")) {
    return JSON.parse(body);
  }

  return body;
}

export default apiFetch;
