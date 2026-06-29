import Link from "next/link";

import { LodgeList } from "@/components/LodgeList";
import { PageContainer } from "@/components/PageContainer";
import { StageList } from "@/components/StageList";
import { TourVariantList } from "@/components/TourVariantList";
import { getLodges } from "@/lib/api/generated/orval/lodges/lodges";
import { getStages } from "@/lib/api/generated/orval/stages/stages";
import { getTourVariants } from "@/lib/api/generated/orval/tour-variants/tour-variants";

export default async function Explore() {
  const [lodgesResponse, stagesResponse, tourVariantsResponse] =
    await Promise.all([
      getLodges({ limit: 3 }),
      getStages({ limit: 3 }),
      getTourVariants({ limit: 3 }),
    ]);

  if (lodgesResponse.status !== 200) {
    throw new Error(`Failed to load lodges: ${lodgesResponse.status}`);
  }

  if (stagesResponse.status !== 200) {
    throw new Error(`Failed to load stages: ${stagesResponse.status}`);
  }

  if (tourVariantsResponse.status !== 200) {
    throw new Error(
      `Failed to load tour variants: ${tourVariantsResponse.status}`,
    );
  }

  return (
    <PageContainer>
      <h1>Explore</h1>

      <section>
        <h2>Lodges</h2>
        <LodgeList lodges={lodgesResponse.data} />
        <Link href="/lodges">View all lodges</Link>
      </section>

      <section>
        <h2>Stages</h2>
        <StageList stages={stagesResponse.data} />
        <Link href="/stages">View all stages</Link>
      </section>

      <section>
        <h2>Tours</h2>
        <TourVariantList variants={tourVariantsResponse.data} />
        <Link href="/tours">View all tours</Link>
      </section>
    </PageContainer>
  );
}
