"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { register } from "@/lib/api/generated/orval/auth/auth";
import { PageContainer } from "@/components/PageContainer";

export default function RegisterPage() {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setError(null);

    const formData = new FormData(event.currentTarget);
    const email = String(formData.get("email") ?? "");

    try {
      await register({
        email,
        password: String(formData.get("password") ?? ""),
      });

      router.push(`/check-email?email=${encodeURIComponent(email)}`);
    } catch (thrown) {
      const responseError = thrown as {
        info?: { detail?: string; errors?: Record<string, string[]> };
      };
      const validationMessage =
        responseError.info?.errors?.Password?.[0] ??
        responseError.info?.errors?.Email?.[0] ??
        responseError.info?.detail;

      setError(validationMessage ?? "Check the form and try again.");
    } finally {
      setPending(false);
    }
  }

  return (
    <PageContainer>
      <main className="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-md flex-col justify-center gap-6">
        <div className="space-y-2">
          <h1>Register</h1>
          <p>Create an account to save lodges as favorites.</p>
        </div>

        <form onSubmit={handleSubmit} className="grid gap-4">
          <label className="grid gap-1">
            <span>Email</span>
            <input name="email" type="email" autoComplete="email" required />
          </label>

          <label className="grid gap-1">
            <span>Password</span>
            <input
              name="password"
              type="password"
              autoComplete="new-password"
              required
            />
          </label>

          {error ? <p role="alert">{error}</p> : null}

          <button type="submit" disabled={pending}>
            {pending ? "Creating account..." : "Register"}
          </button>
        </form>

        <div className="text-sm">
          <Link href="/login">Back to login</Link>
        </div>
      </main>
    </PageContainer>
  );
}
