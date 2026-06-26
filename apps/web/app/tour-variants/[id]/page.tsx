import Link from "next/link";
import { notFound } from "next/navigation";

import { PageContainer } from "@/components/PageContainer";
import { getApiTourVariantsId } from "@/lib/api/generated/orval/tour-variants/tour-variants";

type TourVariantPageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function TourVariant({ params }: TourVariantPageProps) {
  const { id } = await params;
  const { data: variant } = await getApiTourVariantsId(id);

  if (!variant) {
    notFound();
  }

  return (
    <PageContainer>
      <h1>{variant.name}</h1>
      <ul>
        <li>Id: {variant.id}</li>
        <li>
          Tour:{" "}
          <Link href={`/tours/${variant.tour.id}`}>{variant.tour.name}</Link>
        </li>
        <li>Created at: {variant.createdAt}</li>
      </ul>

      <h2>Stages</h2>
      {variant.stages.length === 0 ? (
        <p>No stages found.</p>
      ) : (
        <ul>
          {variant.stages.map((variantStage) => (
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
