import apiClient from "./client";
import type { components } from "./generated/schema";

type StageResponseDto = components["schemas"]["StageResponseDto"];

type ListOptions = {
  limit?: number;
};

export async function getStages(options: ListOptions = {}): Promise<StageResponseDto[]> {
  const { data, error } = await apiClient.GET("/api/stages", {
    params: {
      query: options,
    },
  });

  if (error || !data) {
    throw new Error("Failed to fetch stages");
  }

  return data;
}

export async function getStage(id: number | string): Promise<StageResponseDto | undefined> {
  const { data, error, response } = await apiClient.GET("/api/stages/{id}", {
    params: {
      path: { id },
    },
  });

  if (response.status === 404) {
    return undefined;
  }

  if (error || !data) {
    throw new Error("Failed to fetch stage");
  }

  return data;
}
