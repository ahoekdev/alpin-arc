import apiClient from "./client";
import type { components } from "./generated/schema";

type TourResponseDto = components["schemas"]["TourResponseDto"];
type TourDetailResponseDto = components["schemas"]["TourDetailResponseDto"];

type ListOptions = {
  limit?: number;
};

export async function getTours(options: ListOptions = {}): Promise<TourResponseDto[]> {
  const { data, error } = await apiClient.GET("/api/tours", {
    params: {
      query: options,
    },
  });

  if (error || !data) {
    throw new Error("Failed to fetch tours");
  }

  return data;
}

export async function getTour(id: number | string): Promise<TourDetailResponseDto | undefined> {
  const { data, error, response } = await apiClient.GET("/api/tours/{id}", {
    params: {
      path: { id },
    },
  });

  if (response.status === 404) {
    return undefined;
  }

  if (error || !data) {
    throw new Error("Failed to fetch tour");
  }

  return data;
}
