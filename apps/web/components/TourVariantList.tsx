import Link from "next/link";

import type { TourVariantResponseDto } from "@/lib/api/generated/orval/model";

type TourVariantListProps = {
  variants: TourVariantResponseDto[];
};

function getVariantTitle(variant: TourVariantResponseDto): string {
  return Number(variant.tour.variantCount) > 1
    ? `${variant.tour.name} - ${variant.name}`
    : variant.tour.name;
}

export function TourVariantList({ variants }: TourVariantListProps) {
  if (variants.length === 0) {
    return <p>No tours found.</p>;
  }

  return (
    <ul>
      {variants.map((variant) => {
        const description = variant.description || variant.tour.description;

        return (
          <li key={variant.id}>
            <h3>
              <Link href={`/tour-variants/${variant.id}`}>
                {getVariantTitle(variant)}
              </Link>
            </h3>
            <p>
              Part of{" "}
              <Link href={`/tours/${variant.tour.id}`}>
                {variant.tour.name}
              </Link>
            </p>
            {description && <p>{description}</p>}
            <p>
              {variant.stageCount} stages, {variant.totalDurationMinutes} min,{" "}
              {variant.totalDistanceMeters} m
            </p>
          </li>
        );
      })}
    </ul>
  );
}
