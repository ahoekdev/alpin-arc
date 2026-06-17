import Link from "next/link";

import { getLodges } from "@/lib/api/lodges";

export default async function Lodges() {
  const lodges = await getLodges();

  return (
    <div className="container mx-auto px-4">
      <h1 className="text-3xl font-bold">Lodges</h1>
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
    </div>
  );
}
