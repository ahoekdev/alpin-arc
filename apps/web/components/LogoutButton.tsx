"use client";

import { LogOut } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { logout } from "@/lib/api/generated/orval/auth/auth";

export function LogoutButton() {
  const router = useRouter();
  const [pending, setPending] = useState(false);

  async function handleLogout() {
    setPending(true);

    try {
      await logout();
      router.refresh();
      router.push("/");
    } finally {
      setPending(false);
    }
  }

  return (
    <button
      type="button"
      onClick={handleLogout}
      disabled={pending}
      className="inline-flex items-center gap-2"
    >
      <LogOut className="h-4 w-4" aria-hidden="true" />
      <span>{pending ? "Signing out..." : "Logout"}</span>
    </button>
  );
}
