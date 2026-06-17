import Link from "next/link";
import { notFound } from "next/navigation";

import { getTour } from "@/lib/api/tours";

type TourPageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function Tour({ params }: TourPageProps) {
  const { id } = await params;
  const tour = await getTour(id);

  if (!tour) {
    notFound();
  }

  return (
    <div className="container mx-auto px-4">
      <h1 className="text-3xl font-bold">{tour.name}</h1>
      <ul>
        <li>Id: {tour.id}</li>
        <li>Created at: {tour.createdAt}</li>
      </ul>

      <h2>Variants</h2>
      {tour.variants.length === 0 ? (
        <p>No variants found.</p>
      ) : (
        <ul>
          {tour.variants.map((variant) => (
            <li key={variant.id}>
              <Link href={`/tour-variants/${variant.id}`}>{variant.name}</Link>
              : {variant.stageCount} stages, {variant.totalDurationMinutes} min,{" "}
              {variant.totalDistanceMeters} m
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
