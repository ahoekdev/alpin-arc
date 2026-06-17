import Link from "next/link";

import { getStages } from "@/lib/api/stages";
import { PageContainer } from "@/components/PageContainer";

export default async function Stages() {
  const stages = await getStages();

  return (
    <PageContainer>
      <h1>Stages</h1>
      {stages.length === 0 ? (
        <p>No stages found.</p>
      ) : (
        <ul>
          {stages.map((stage) => (
            <li key={stage.id}>
              <Link href={`/stages/${stage.id}`}>
                {`${stage.startLodge.name} > ${stage.endLodge.name}`}
              </Link>
              : {stage.durationMinutes} min, {stage.distanceMeters}m
            </li>
          ))}
        </ul>
      )}
    </PageContainer>
  );
}
