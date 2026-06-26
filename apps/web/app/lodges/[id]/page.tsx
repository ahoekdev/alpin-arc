import Link from "next/link";
import { notFound } from "next/navigation";

import { PageContainer } from "@/components/PageContainer";
import { getLodgeById } from "@/lib/api/generated/orval/lodges/lodges";

type LodgePageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function Lodge({ params }: LodgePageProps) {
  const { id } = await params;
  const { data: lodge } = await getLodgeById(id);

  if (!lodge) {
    notFound();
  }

  const { stages, tours } = lodge;

  return (
    <PageContainer>
      <h1>{lodge.name}</h1>
      <ul>
        <li>Id: {lodge.id}</li>
        <li>Created at: {lodge.createdAt}</li>
      </ul>

      <h2>Stages</h2>
      {!stages.length ? (
        <p>No stages found.</p>
      ) : (
        <ul>
          {stages.map(
            ({ id, startLodge, endLodge, durationMinutes, distanceMeters }) => {
              const startsAtCurrentLodge =
                String(startLodge.id) === String(lodge.id);

              return (
                <li key={id}>
                  {startsAtCurrentLodge ? (
                    <>
                      {`${startLodge.name} > `}
                      <Link className="italic" href={`/lodges/${endLodge.id}`}>
                        {endLodge.name}
                      </Link>
                    </>
                  ) : (
                    <>
                      <Link
                        className="italic"
                        href={`/lodges/${startLodge.id}`}
                      >
                        {startLodge.name}
                      </Link>
                      {` > ${endLodge.name}`}
                    </>
                  )}
                  : {durationMinutes} min, {distanceMeters} m
                </li>
              );
            },
          )}
        </ul>
      )}

      <h2>Tours</h2>
      {!tours.length ? (
        <p>No tours found.</p>
      ) : (
        <ul>
          {tours.map(({ id, name }) => (
            <li key={id}>
              <Link href={`/tours/${id}`}>{name}</Link>
            </li>
          ))}
        </ul>
      )}
    </PageContainer>
  );
}
