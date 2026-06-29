import { LodgeList } from "@/components/LodgeList";
import { PageContainer } from "@/components/PageContainer";
import { getLodges } from "@/lib/api/generated/orval/lodges/lodges";

export default async function Lodges() {
  const response = await getLodges();

  if (response.status !== 200) {
    throw Error("There was a problem fetching lodges");
  }

  return (
    <PageContainer>
      <h1>Lodges</h1>
      <LodgeList lodges={response.data} />
    </PageContainer>
  );
}
