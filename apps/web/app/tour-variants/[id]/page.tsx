import Link from "next/link";
import { notFound } from "next/navigation";

import { PageContainer } from "@/components/PageContainer";
import { StageList } from "@/components/StageList";
import { getTourVariantById } from "@/lib/api/generated/orval/tour-variants/tour-variants";

type TourVariantPageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function TourVariant({ params }: TourVariantPageProps) {
  const { id: variantId } = await params;

  const response = await getTourVariantById(variantId);

  if (response.status === 404) {
    notFound();
  }

  const variant = response.data;
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
      <StageList stages={stages.map((variantStage) => variantStage.stage)} />
    </PageContainer>
  );
}
