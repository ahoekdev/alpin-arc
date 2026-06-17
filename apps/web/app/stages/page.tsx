import Link from "next/link";

import { getStages } from "@/lib/api/stages";

export default async function Stages() {
  const stages = await getStages();

  return (
    <div className="container mx-auto px-4">
      <h1 className="text-3xl font-bold">Stages</h1>
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
    </div>
  );
}
