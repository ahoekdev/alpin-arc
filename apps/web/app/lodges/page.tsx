import Link from "next/link";

import { PageContainer } from "@/components/PageContainer";
import { getLodges } from "@/lib/api/generated/orval/lodges/lodges";

export default async function Lodges() {
  const { data: lodges } = await getLodges();

  return (
    <PageContainer>
      <h1>Lodges</h1>
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
    </PageContainer>
  );
}
