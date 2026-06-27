import Link from "next/link";

import { PageContainer } from "@/components/PageContainer";
import { getTours } from "@/lib/api/generated/orval/tours/tours";

export default async function Tours() {
  const { data: tours } = await getTours();

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
              {tour.description ? <p>{tour.description}</p> : null}
            </li>
          ))}
        </ul>
      )}
    </PageContainer>
  );
}
