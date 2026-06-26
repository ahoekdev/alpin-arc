import { PageContainer } from "@/components/PageContainer";
import { StageList } from "@/components/StageList";
import { getStages } from "@/lib/api/generated/orval/stages/stages";

export default async function Stages() {
  const { data: stages } = await getStages();

  return (
    <PageContainer>
      <h1>Stages</h1>
      <StageList stages={stages} />
    </PageContainer>
  );
}
