"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { confirmEmail } from "@/lib/api/generated/orval/auth/auth";
import { PageContainer } from "@/components/PageContainer";

export default function ConfirmEmailPage() {
  const searchParams = useSearchParams();
  const started = useRef(false);
  const [state, setState] = useState<"pending" | "success" | "error">("pending");

  useEffect(() => {
    if (started.current) {
      return;
    }

    started.current = true;

    const userId = searchParams.get("userId");
    const code = searchParams.get("code");

    if (!userId || !code) {
      setState("error");
      return;
    }

    void confirmEmail({ userId, code })
      .then(() => setState("success"))
      .catch(() => setState("error"));
  }, [searchParams]);

  return (
    <PageContainer>
      <main className="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-md flex-col justify-center gap-6">
        <div className="space-y-2">
          <h1>Confirm email</h1>
          {state === "pending" ? <p>Confirming your email...</p> : null}
          {state === "success" ? <p>Your email is confirmed.</p> : null}
          {state === "error" ? (
            <p>The confirmation link is invalid or expired.</p>
          ) : null}
        </div>

        {state === "success" ? <Link href="/login">Login</Link> : null}
        {state === "error" ? (
          <div className="flex gap-4 text-sm">
            <Link href="/resend-confirmation">Resend confirmation</Link>
            <Link href="/login">Back to login</Link>
          </div>
        ) : null}
      </main>
    </PageContainer>
  );
}
