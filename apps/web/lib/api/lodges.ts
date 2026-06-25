import apiClient from "./client";
import type { components } from "./generated/schema";

type Lodge = components["schemas"]["Lodge"];

type ListOptions = {
  limit?: number;
};

export async function getLodges(options: ListOptions = {}): Promise<Lodge[]> {
  const { data, error } = await apiClient.GET("/api/lodges", {
    params: { query: options },
  });

  if (error || !data) {
    throw new Error("Failed to fetch lodges");
  }

  return data;
}

export async function getLodge(
  id: number | string,
): Promise<Lodge | undefined> {
  const { data, error, response } = await apiClient.GET("/api/lodges/{id}", {
    params: { path: { id } },
  });

  if (response.status === 404) {
    return undefined;
  }

  if (error || !data) {
    throw new Error("Failed to fetch lodge");
  }

  return data;
}
