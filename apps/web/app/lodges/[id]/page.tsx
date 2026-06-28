import Link from "next/link";
import { notFound } from "next/navigation";

import { PageContainer } from "@/components/PageContainer";
import { StageList } from "@/components/StageList";
import { getLodgeById } from "@/lib/api/generated/orval/lodges/lodges";

type LodgePageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function Lodge({ params }: LodgePageProps) {
  const { id } = await params;
  const response = await getLodgeById(id);

  if (response.status === 404) {
    notFound();
  }

  const lodge = response.data;
  const { stages, tours } = lodge;

  return (
    <PageContainer>
      <h1>{lodge.name}</h1>
      <ul>
        <li>Id: {lodge.id}</li>
        <li>Created at: {lodge.createdAt}</li>
      </ul>

      <h2>Stages</h2>
      <StageList stages={stages} />

      <h2>Tours</h2>
      {!tours.length ? (
        <p>No tours found.</p>
      ) : (
        <ul>
          {tours.map(({ id, name, description }) => (
            <li key={id}>
              <Link href={`/tours/${id}`}>{name}</Link>
              {description ? <p>{description}</p> : null}
            </li>
          ))}
        </ul>
      )}
    </PageContainer>
  );
}
