import Link from "next/link";

import { getTours } from "@/lib/api/tours";

export default async function Tours() {
  const tours = await getTours();

  return (
    <div className="container mx-auto px-4">
      <h1>Tours</h1>
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
    </div>
  );
}
