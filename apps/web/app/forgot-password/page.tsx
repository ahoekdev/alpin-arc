"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { forgotPassword } from "@/lib/api/generated/orval/auth/auth";
import { PageContainer } from "@/components/PageContainer";

export default function ForgotPasswordPage() {
  const [pending, setPending] = useState(false);
  const [sent, setSent] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);

    const formData = new FormData(event.currentTarget);

    try {
      await forgotPassword({
        email: String(formData.get("email") ?? ""),
      });
      setSent(true);
    } finally {
      setPending(false);
    }
  }

  return (
    <PageContainer>
      <main className="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-md flex-col justify-center gap-6">
        <div className="space-y-2">
          <h1>Forgot password</h1>
          <p>Request a password reset link by email.</p>
        </div>

        {sent ? (
          <div className="grid gap-4">
            <p>If an account exists, we sent reset instructions.</p>
            <Link href="/login">Back to login</Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="grid gap-4">
            <label className="grid gap-1">
              <span>Email</span>
              <input name="email" type="email" autoComplete="email" required />
            </label>

            <button type="submit" disabled={pending}>
              {pending ? "Sending..." : "Send reset link"}
            </button>
          </form>
        )}
      </main>
    </PageContainer>
  );
}
