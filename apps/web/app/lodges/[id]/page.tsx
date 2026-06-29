import { notFound } from "next/navigation";

import { PageContainer } from "@/components/PageContainer";
import { StageList } from "@/components/StageList";
import { TourVariantList } from "@/components/TourVariantList";
import { getLodgeById } from "@/lib/api/generated/orval/lodges/lodges";
import { getTourVariants } from "@/lib/api/generated/orval/tour-variants/tour-variants";

type LodgePageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function Lodge({ params }: LodgePageProps) {
  const { id } = await params;
  const [lodgeResponse, tourVariantsResponse] = await Promise.all([
    getLodgeById(id),
    getTourVariants({ lodgeId: id }),
  ]);

  if (lodgeResponse.status === 404 || tourVariantsResponse.status === 404) {
    notFound();
  }

  if (tourVariantsResponse.status !== 200) {
    throw new Error(
      `Failed to load tour variants: ${tourVariantsResponse.status}`,
    );
  }

  const lodge = lodgeResponse.data;
  const { stages } = lodge;

  return (
    <PageContainer>
      <h1>{lodge.name}</h1>
      <p>{lodge.description}</p>
      <ul>
        <li>Id: {lodge.id}</li>
        <li>Country: {lodge.countryCode}</li>
        <li>Created at: {lodge.createdAt}</li>
      </ul>

      <h2>Stages</h2>
      <StageList stages={stages} />

      <h2>Tours</h2>
      <TourVariantList variants={tourVariantsResponse.data} />
    </PageContainer>
  );
}
