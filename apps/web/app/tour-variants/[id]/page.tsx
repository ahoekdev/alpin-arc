import Link from "next/link";
import { notFound } from "next/navigation";

import { PageContainer } from "@/components/PageContainer";
import { getTourVariantById } from "@/lib/api/generated/orval/tour-variants/tour-variants";

type TourVariantPageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function TourVariant({ params }: TourVariantPageProps) {
  const { id: variantId } = await params;

  const { data: variant } = await getTourVariantById(variantId);

  if (!variant) {
    notFound();
  }

  const { name, description, stages, id, tour } = variant;

  return (
    <PageContainer>
      <h1>{name}</h1>
      {description ? <p>{description}</p> : null}
      <ul>
        <li>Id: {id}</li>
        <li>
          Tour: <Link href={`/tours/${tour.id}`}>{tour.name}</Link>
        </li>
      </ul>

      <h2>Stages</h2>
      {!stages.length ? (
        <p>No stages found.</p>
      ) : (
        <ul>
          {stages.map((variantStage) => (
            <li key={variantStage.id}>
              {variantStage.order}.{" "}
              <Link href={`/stages/${variantStage.stage.id}`}>
                {`${variantStage.stage.startLodge.name} > ${variantStage.stage.endLodge.name}`}
              </Link>
              : {variantStage.stage.durationMinutes} min,{" "}
              {variantStage.stage.distanceMeters} m
            </li>
          ))}
        </ul>
      )}
    </PageContainer>
  );
}
