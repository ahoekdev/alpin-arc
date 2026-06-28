import { PageContainer } from "@/components/PageContainer";
import { Spinner } from "@/components/ui/spinner";

interface LoadingPageProps {
  title: string;
}

export default function LoadingPage({ title }: LoadingPageProps) {
  return (
    <PageContainer>
      <div className="flex flex-col items-center mt-20 gap-8">
        <Spinner />
        <p>{title}</p>
      </div>
    </PageContainer>
  );
}
