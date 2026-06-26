import Link from "next/link";

type StageListLodge = {
  id: number | string;
  name: string;
};

export type StageListItem = {
  id: number | string;
  startLodge: StageListLodge;
  endLodge: StageListLodge;
  durationMinutes: number | string;
  distanceMeters: number | string;
};

type StageListProps = {
  stages: StageListItem[];
};

export function StageList({ stages }: StageListProps) {
  if (stages.length === 0) {
    return <p>No stages found.</p>;
  }

  return (
    <ul>
      {stages.map((stage) => (
        <li key={stage.id}>
          <Link href={`/lodges/${stage.startLodge.id}`}>
            {stage.startLodge.name}
          </Link>
          {" > "}
          <Link href={`/lodges/${stage.endLodge.id}`}>
            {stage.endLodge.name}
          </Link>
          : {stage.durationMinutes} min, {stage.distanceMeters} m{" "}
          <Link href={`/stages/${stage.id}`}>Stage details</Link>
        </li>
      ))}
    </ul>
  );
}
