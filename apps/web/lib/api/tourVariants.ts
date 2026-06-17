import apiClient from "./client";
import type { components } from "./generated/schema";

type TourVariantDetailResponseDto = components["schemas"]["TourVariantDetailResponseDto"];

export async function getTourVariant(id: number | string): Promise<TourVariantDetailResponseDto | undefined> {
  const { data, error, response } = await apiClient.GET("/api/tour-variants/{id}", {
    params: {
      path: { id },
    },
  });

  if (response.status === 404) {
    return undefined;
  }

  if (error || !data) {
    throw new Error("Failed to fetch tour variant");
  }

  return data;
}
