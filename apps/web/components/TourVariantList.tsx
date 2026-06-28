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
    <ul className="space-y-3">
      {variants.map((variant) => {
        const description = variant.description || variant.tour.description;

        return (
          <li key={variant.id} className="border-b border-border pb-3">
            <p className="font-medium">
              <Link href={`/tour-variants/${variant.id}`}>
                {getVariantTitle(variant)}
              </Link>
            </p>
            <p className="text-sm text-muted-foreground">
              Part of{" "}
              <Link href={`/tours/${variant.tour.id}`}>
                {variant.tour.name}
              </Link>
            </p>
            {description && <p className="mt-1">{description}</p>}
            <p className="mt-1 text-sm text-muted-foreground">
              {variant.stageCount} stages, {variant.totalDurationMinutes} min,{" "}
              {variant.totalDistanceMeters} m
            </p>
          </li>
        );
      })}
    </ul>
  );
}
