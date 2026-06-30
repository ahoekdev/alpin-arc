"use client";

import { Heart } from "lucide-react";
import { useState } from "react";
import {
  addMyLodgeFavorite,
  removeMyLodgeFavorite,
} from "@/lib/api/generated/orval/me-lodge-favorites/me-lodge-favorites";

type FavoriteLodgeButtonProps = {
  lodgeId: number;
  initialIsFavorite: boolean;
};

export function FavoriteLodgeButton({
  lodgeId,
  initialIsFavorite,
}: FavoriteLodgeButtonProps) {
  const [isFavorite, setIsFavorite] = useState(initialIsFavorite);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function toggleFavorite() {
    const nextValue = !isFavorite;
    setIsFavorite(nextValue);
    setPending(true);
    setError(null);

    try {
      if (nextValue) {
        await addMyLodgeFavorite(lodgeId);
      } else {
        await removeMyLodgeFavorite(lodgeId);
      }
    } catch {
      setIsFavorite(!nextValue);
      setError("Could not update your favorite. Try again.");
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="grid gap-2">
      <button
        type="button"
        onClick={toggleFavorite}
        disabled={pending}
        aria-pressed={isFavorite}
        className="inline-flex items-center gap-2"
      >
        <Heart
          className="h-4 w-4"
          aria-hidden="true"
          fill={isFavorite ? "currentColor" : "none"}
        />
        <span>{pending ? "Updating..." : isFavorite ? "Saved" : "Save"}</span>
      </button>
      {error ? <p role="alert">{error}</p> : null}
    </div>
  );
}
