"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, useState } from "react";
import { login } from "@/lib/api/generated/orval/auth/auth";
import { PageContainer } from "@/components/PageContainer";

function getSafeReturnUrl(value: string | null): string {
  if (!value || !value.startsWith("/") || value.startsWith("//")) {
    return "/";
  }

  return value;
}

export default function LoginPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setError(null);

    const formData = new FormData(event.currentTarget);

    try {
      await login({
        email: String(formData.get("email") ?? ""),
        password: String(formData.get("password") ?? ""),
        rememberMe: formData.get("rememberMe") === "on",
      });

      router.refresh();
      router.push(getSafeReturnUrl(searchParams.get("returnUrl")));
    } catch (thrown) {
      const responseError = thrown as {
        status?: number;
        info?: { detail?: string };
      };

      setError(responseError.info?.detail ?? "Invalid email or password.");
    } finally {
      setPending(false);
    }
  }

  return (
    <PageContainer>
      <main className="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-md flex-col justify-center gap-6">
        <div className="space-y-2">
          <h1>Login</h1>
          <p>Sign in to save your favorite lodges.</p>
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
              autoComplete="current-password"
              required
            />
          </label>

          <label className="flex items-center gap-2">
            <input name="rememberMe" type="checkbox" />
            <span>Remember me</span>
          </label>

          {error ? <p role="alert">{error}</p> : null}

          <button type="submit" disabled={pending}>
            {pending ? "Signing in..." : "Login"}
          </button>
        </form>

        <div className="flex gap-4 text-sm">
          <Link href="/register">Create account</Link>
          <Link href="/forgot-password">Forgot password</Link>
        </div>
      </main>
    </PageContainer>
  );
}
