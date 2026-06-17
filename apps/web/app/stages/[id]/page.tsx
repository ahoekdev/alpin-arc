import { notFound } from "next/navigation";

import { getStage } from "@/lib/api/stages";

type StagePageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function Stage({ params }: StagePageProps) {
  const { id } = await params;
  const stage = await getStage(id);

  if (!stage) {
    notFound();
  }

  return (
    <div className="container mx-auto px-4">
      <h1>
        {stage.startLodge.name} -&gt; {stage.endLodge.name}
      </h1>
      <ul>
        <li>Id: {stage.id}</li>
        <li>Start lodge: {stage.startLodge.name}</li>
        <li>End lodge: {stage.endLodge.name}</li>
        <li>Duration: {stage.durationMinutes} min</li>
        <li>Distance: {stage.distanceMeters} m</li>
        <li>Created at: {stage.createdAt}</li>
      </ul>
    </div>
  );
}
