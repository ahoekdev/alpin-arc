import { notFound } from "next/navigation";

import { getLodge } from "@/lib/api/lodges";
import { PageContainer } from "@/components/PageContainer";

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
    <PageContainer>
      <h1>{lodge.name}</h1>
      <ul>
        <li>Id: {lodge.id}</li>
        <li>Created at: {lodge.createdAt}</li>
      </ul>
    </PageContainer>
  );
}
