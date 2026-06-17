import { notFound } from "next/navigation";

import { getLodge } from "@/lib/api/lodges";

type LodgePageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function Lodge({ params }: LodgePageProps) {
  const { id } = await params;
  const lodge = await getLodge(id);

  if (!lodge) {
    notFound();
  }

  return (
    <div className="container mx-auto px-4">
      <h1>{lodge.name}</h1>
      <ul>
        <li>Id: {lodge.id}</li>
        <li>Created at: {lodge.createdAt}</li>
      </ul>
    </div>
  );
}
