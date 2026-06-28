import { PageContainer } from "@/components/PageContainer";
import { TourVariantList } from "@/components/TourVariantList";
import { getTourVariants } from "@/lib/api/generated/orval/tour-variants/tour-variants";

export default async function Tours() {
  const response = await getTourVariants();

  if (response.status !== 200) {
    throw new Error(`Failed to load tour variants: ${response.status}`);
  }

  return (
    <PageContainer>
      <h1>Tours</h1>
      <TourVariantList variants={response.data} />
    </PageContainer>
  );
}
