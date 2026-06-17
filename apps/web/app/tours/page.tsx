import Link from "next/link";

import { getTours } from "@/lib/api/tours";
import { PageContainer } from "@/components/PageContainer";

export default async function Tours() {
  const tours = await getTours();

  return (
    <PageContainer>
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
    </PageContainer>
  );
}
