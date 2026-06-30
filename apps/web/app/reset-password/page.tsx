"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { FormEvent, useState } from "react";
import { resetPassword } from "@/lib/api/generated/orval/auth/auth";
import { PageContainer } from "@/components/PageContainer";

export default function ResetPasswordPage() {
  const searchParams = useSearchParams();
  const email = searchParams.get("email");
  const code = searchParams.get("code");
  const [pending, setPending] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setError(null);

    const formData = new FormData(event.currentTarget);

    if (!email || !code) {
      setError("The reset link is invalid or expired.");
      setPending(false);
      return;
    }

    try {
      await resetPassword({
        email,
        code,
        password: String(formData.get("password") ?? ""),
      });
      setSuccess(true);
    } catch (thrown) {
      const responseError = thrown as {
        info?: { detail?: string; errors?: Record<string, string[]> };
      };

      setError(
        responseError.info?.errors?.Password?.[0] ??
          responseError.info?.detail ??
          "The reset link is invalid or expired.",
      );
    } finally {
      setPending(false);
    }
  }

  return (
    <PageContainer>
      <main className="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-md flex-col justify-center gap-6">
        <div className="space-y-2">
          <h1>Reset password</h1>
          <p>Set a new password for your account.</p>
        </div>

        {success ? (
          <div className="grid gap-4">
            <p>Your password has been reset.</p>
            <Link href="/login">Login</Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="grid gap-4">
            <label className="grid gap-1">
              <span>New password</span>
              <input
                name="password"
                type="password"
                autoComplete="new-password"
                required
              />
            </label>

            {error ? <p role="alert">{error}</p> : null}

            <button type="submit" disabled={pending}>
              {pending ? "Resetting..." : "Reset password"}
            </button>
          </form>
        )}
      </main>
    </PageContainer>
  );
}
