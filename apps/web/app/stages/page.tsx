import { getStages } from "@/lib/api/stages";

export default async function Stages() {
  const stages = await getStages();

  return (
    <div className="container mx-auto px-4">
      <h1 className="text-3xl font-bold">Stages</h1>
      <ul>
        {stages.map((stage) => (
          <li key={stage.id}>
            {stage.startLodge.name} -&gt; {stage.endLodge.name}:{" "}
            {stage.durationMinutes} min, {stage.distanceMeters} m
          </li>
        ))}
      </ul>
    </div>
  );
}
