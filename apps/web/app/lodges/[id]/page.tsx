import { notFound } from "next/navigation";
import { FavoriteLodgeButton } from "@/components/FavoriteLodgeButton";
import { PageContainer } from "@/components/PageContainer";
import { StageList } from "@/components/StageList";
import { TourVariantList } from "@/components/TourVariantList";
import { getCurrentUser } from "@/lib/api/generated/orval/auth/auth";
import { getLodgeById } from "@/lib/api/generated/orval/lodges/lodges";
import { getMyLodgeFavoriteState } from "@/lib/api/generated/orval/me-lodge-favorites/me-lodge-favorites";
import { getTourVariants } from "@/lib/api/generated/orval/tour-variants/tour-variants";
import { getServerCookieHeader } from "@/lib/api/server";

type LodgePageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function Lodge({ params }: LodgePageProps) {
  const { id } = await params;
  const cookie = await getServerCookieHeader();
  const [lodgeResponse, tourVariantsResponse, currentUserResponse] =
    await Promise.all([
    getLodgeById(id),
    getTourVariants({ lodgeId: id }),
    getCurrentUser(cookie ? { headers: { Cookie: cookie } } : undefined),
  ]);

  if (lodgeResponse.status === 404 || tourVariantsResponse.status === 404) {
    notFound();
  }

  if (tourVariantsResponse.status !== 200) {
    throw new Error(
      `Failed to load tour variants: ${tourVariantsResponse.status}`,
    );
  }

  const lodge = lodgeResponse.data;
  const { stages } = lodge;
  const currentUser = currentUserResponse.data;
  const favoriteState = currentUser.isAuthenticated
    ? await getMyLodgeFavoriteState(id, cookie ? { headers: { Cookie: cookie } } : undefined)
    : null;

  if (currentUser.isAuthenticated && favoriteState?.status !== 200) {
    throw new Error(`Failed to load favorite state: ${favoriteState?.status}`);
  }

  return (
    <PageContainer>
      <h1>{lodge.name}</h1>
      <p>{lodge.description}</p>
      {currentUser.isAuthenticated && favoriteState?.status === 200 ? (
        <FavoriteLodgeButton
          lodgeId={Number(lodge.id)}
          initialIsFavorite={favoriteState.data.isFavorite}
        />
      ) : null}
      <ul>
        <li>Id: {lodge.id}</li>
        <li>Country: {lodge.countryCode}</li>
        <li>Created at: {lodge.createdAt}</li>
      </ul>

      <h2>Stages</h2>
      <StageList stages={stages} />

      <h2>Tours</h2>
      <TourVariantList variants={tourVariantsResponse.data} />
    </PageContainer>
  );
}
