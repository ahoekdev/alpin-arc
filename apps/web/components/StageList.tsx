import Link from "next/link";

type StageListStage = {
  id: number | string;
  startLodge: {
    id: number | string;
    name: string;
  };
  endLodge: {
    id: number | string;
    name: string;
  };
  durationMinutes: number | string;
  distanceMeters: number | string;
};

type StageListProps = {
  stages: StageListStage[];
};

export function StageList({ stages }: StageListProps) {
  if (stages.length === 0) {
    return <p>No stages found.</p>;
  }

  return (
    <ul className="space-y-3">
      {stages.map((stage) => (
        <li key={stage.id} className="border-b border-border pb-3">
          <p className="font-medium">
            <Link href={`/stages/${stage.id}`}>
              {stage.startLodge.name} &gt; {stage.endLodge.name}
            </Link>
          </p>
          <p className="text-sm text-muted-foreground">
            From{" "}
            <Link href={`/lodges/${stage.startLodge.id}`}>
              {stage.startLodge.name}
            </Link>{" "}
            to{" "}
            <Link href={`/lodges/${stage.endLodge.id}`}>
              {stage.endLodge.name}
            </Link>
          </p>
          <p className="mt-1 text-sm text-muted-foreground">
            {stage.durationMinutes} min, {stage.distanceMeters} m
          </p>
        </li>
      ))}
    </ul>
  );
}
