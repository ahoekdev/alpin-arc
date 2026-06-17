import Link from "next/link";

import { getLodges } from "@/lib/api/lodges";
import { getStages } from "@/lib/api/stages";
import { getTours } from "@/lib/api/tours";

export default async function Explore() {
  const [lodges, stages, tours] = await Promise.all([
    getLodges({ limit: 3 }),
    getStages({ limit: 3 }),
    getTours({ limit: 3 }),
  ]);

  return (
    <div className="container mx-auto px-4">
      <h1>Explore</h1>

      <section>
        <h2>Lodges</h2>
        {lodges.length === 0 ? (
          <p>No lodges found.</p>
        ) : (
          <ul>
            {lodges.map((lodge) => (
              <li key={lodge.id}>
                <Link href={`/lodges/${lodge.id}`}>{lodge.name}</Link>
              </li>
            ))}
          </ul>
        )}
        <Link href="/lodges">View all lodges</Link>
      </section>

      <section>
        <h2>Stages</h2>
        {stages.length === 0 ? (
          <p>No stages found.</p>
        ) : (
          <ul>
            {stages.map((stage) => (
              <li key={stage.id}>
                <Link href={`/stages/${stage.id}`}>
                  {stage.startLodge.name} -&gt; {stage.endLodge.name}
                </Link>
                : {stage.durationMinutes} min, {stage.distanceMeters} m
              </li>
            ))}
          </ul>
        )}
        <Link href="/stages">View all stages</Link>
      </section>

      <section>
        <h2>Tours</h2>
        {tours.length === 0 ? (
          <p>No tours found.</p>
        ) : (
          <ul>
            {tours.map((tour) => (
              <li key={tour.id}>
                <Link href={`/tours/${tour.id}`}>{tour.name}</Link>
              </li>
            ))}
          </ul>
        )}
        <Link href="/tours">View all tours</Link>
      </section>
    </div>
  );
}
