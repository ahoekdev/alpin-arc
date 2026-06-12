import apiClient from "./client";
import type { components } from "./generated/schema";

type StageResponseDto = components["schemas"]["StageResponseDto"];

export async function getStages(): Promise<StageResponseDto[]> {
  const { data, error } = await apiClient.GET("/api/stages");

  if (error || !data) {
    throw new Error("Failed to fetch stages");
  }

  return data;
}
