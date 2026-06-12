import apiClient from "./client";
import type { components } from "./generated/schema";

type Lodge = components["schemas"]["Lodge"];

export async function getLodges(): Promise<Lodge[]> {
  const { data, error } = await apiClient.GET("/api/Lodges");

  if (error || !data) {
    throw new Error("Failed to fetch lodges");
  }

  return data;
}
