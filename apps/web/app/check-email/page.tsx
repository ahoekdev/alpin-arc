import Link from "next/link";
import { PageContainer } from "@/components/PageContainer";

type CheckEmailPageProps = {
  searchParams: Promise<{
    email?: string;
  }>;
};

export default async function CheckEmailPage({
  searchParams,
}: CheckEmailPageProps) {
  const { email } = await searchParams;

  return (
    <PageContainer>
      <main className="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-md flex-col justify-center gap-6">
        <div className="space-y-2">
          <h1>Check your email</h1>
          <p>
            If this email can be used, we sent a confirmation link
            {email ? ` to ${email}` : ""}.
          </p>
        </div>

        <div className="flex gap-4 text-sm">
          <Link href="/resend-confirmation">Resend confirmation</Link>
          <Link href="/login">Back to login</Link>
        </div>
      </main>
    </PageContainer>
  );
}
