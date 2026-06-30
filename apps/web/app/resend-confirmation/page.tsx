"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { resendConfirmation } from "@/lib/api/generated/orval/auth/auth";
import { PageContainer } from "@/components/PageContainer";

export default function ResendConfirmationPage() {
  const [pending, setPending] = useState(false);
  const [sent, setSent] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);

    const formData = new FormData(event.currentTarget);

    try {
      await resendConfirmation({
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
          <h1>Resend confirmation</h1>
          <p>Request a new email confirmation link.</p>
        </div>

        {sent ? (
          <div className="grid gap-4">
            <p>If this email can be used, we sent a confirmation link.</p>
            <Link href="/login">Back to login</Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="grid gap-4">
            <label className="grid gap-1">
              <span>Email</span>
              <input name="email" type="email" autoComplete="email" required />
            </label>

            <button type="submit" disabled={pending}>
              {pending ? "Sending..." : "Send confirmation link"}
            </button>
          </form>
        )}
      </main>
    </PageContainer>
  );
}
