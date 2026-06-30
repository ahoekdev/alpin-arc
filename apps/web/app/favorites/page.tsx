import { redirect } from "next/navigation";
import { LodgeList } from "@/components/LodgeList";
import { PageContainer } from "@/components/PageContainer";
import { getCurrentUser } from "@/lib/api/generated/orval/auth/auth";
import { getMyLodgeFavorites } from "@/lib/api/generated/orval/me-lodge-favorites/me-lodge-favorites";
import { getServerCookieHeader } from "@/lib/api/server";

export default async function FavoritesPage() {
  const cookie = await getServerCookieHeader();
  const currentUserResponse = await getCurrentUser(
    cookie ? { headers: { Cookie: cookie } } : undefined,
  );

  if (!currentUserResponse.data.isAuthenticated) {
    redirect("/login?returnUrl=/favorites");
  }

  const favoritesResponse = await getMyLodgeFavorites(
    cookie ? { headers: { Cookie: cookie } } : undefined,
  );

  if (favoritesResponse.status !== 200) {
    throw new Error(`Failed to load favorites: ${favoritesResponse.status}`);
  }

  const favorites = favoritesResponse.data;

  return (
    <PageContainer>
      <main className="grid gap-6">
        <div className="space-y-2">
          <h1>Favorites</h1>
          <p>Your saved lodges, sorted by name.</p>
        </div>

        {favorites.length > 0 ? (
          <LodgeList lodges={favorites} />
        ) : (
          <p>You have not saved any lodges yet.</p>
        )}
      </main>
    </PageContainer>
  );
}
