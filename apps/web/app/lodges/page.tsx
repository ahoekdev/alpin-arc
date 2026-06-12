import { getLodges } from "@/lib/api/lodges";

export default async function Lodges() {
  const lodges = await getLodges();

  return (
    <div className="container mx-auto px-4">
      <h1 className="text-3xl font-bold">Lodges</h1>
      <ul>
        {lodges.map((lodge) => (
          <li key={lodge.id}>{lodge.name}</li>
        ))}
      </ul>
    </div>
  );
}
