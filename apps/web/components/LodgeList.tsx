import Link from "next/link";

import type { LodgeSummaryDto } from "@/lib/api/generated/orval/model";

type LodgeListProps = {
  lodges: LodgeSummaryDto[];
};

export function LodgeList({ lodges }: LodgeListProps) {
  if (lodges.length === 0) {
    return <p>No lodges found.</p>;
  }

  return (
    <ul className="space-y-3">
      {lodges.map((lodge) => (
        <li key={lodge.id} className="border-b border-border pb-3">
          <p className="font-medium">
            <Link href={`/lodges/${lodge.id}`}>{lodge.name}</Link>
          </p>
          <p className="text-sm text-muted-foreground">
            {lodge.countryCode}
          </p>
          {lodge.description && <p className="mt-1">{lodge.description}</p>}
        </li>
      ))}
    </ul>
  );
}
