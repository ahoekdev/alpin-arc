import Link from "next/link";
import { notFound } from "next/navigation";

import { PageContainer } from "@/components/PageContainer";
import { getApiToursId } from "@/lib/api/generated/orval/tours/tours";

type TourPageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function Tour({ params }: TourPageProps) {
  const { id } = await params;
  const { data: tour } = await getApiToursId(id);

  if (!tour) {
    notFound();
  }

  return (
    <PageContainer>
      <h1>{tour.name}</h1>
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
              <Link href={`/tour-variants/${variant.id}`}>{variant.name}</Link>:{" "}
              {variant.stageCount} stages, {variant.totalDurationMinutes} min,{" "}
              {variant.totalDistanceMeters} m
            </li>
          ))}
        </ul>
      )}
    </PageContainer>
  );
}
