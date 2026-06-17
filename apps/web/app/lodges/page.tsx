import Link from "next/link";

import { getLodges } from "@/lib/api/lodges";
import { PageContainer } from "@/components/PageContainer";

export default async function Lodges() {
  const lodges = await getLodges();

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
