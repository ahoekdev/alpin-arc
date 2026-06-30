import Link from "next/link";
import { getCurrentUser } from "@/lib/api/generated/orval/auth/auth";
import { getServerCookieHeader } from "@/lib/api/server";
import { LogoutButton } from "./LogoutButton";
import { PageContainer } from "./PageContainer";

export default async function SiteHeader() {
  const cookie = await getServerCookieHeader();
  const currentUserResponse = await getCurrentUser(
    cookie ? { headers: { Cookie: cookie } } : undefined,
  );
  const currentUser = currentUserResponse.data;

  return (
    <header className="h-16 flex items-center border-b border-gray-200">
      <PageContainer>
        <nav>
          <ul className="flex items-center gap-4">
            <li>
              <Link href="/">Home</Link>
            </li>
            <li>
              <Link href="/explore">Explore</Link>
            </li>
            <li>
              <Link href="/tours">Tours</Link>
            </li>
            {currentUser.isAuthenticated ? (
              <>
                <li>
                  <Link href="/favorites">Favorites</Link>
                </li>
                <li>{currentUser.email}</li>
                <li>
                  <LogoutButton />
                </li>
              </>
            ) : (
              <>
                <li>
                  <Link href="/login">Login</Link>
                </li>
                <li>
                  <Link href="/register">Register</Link>
                </li>
              </>
            )}
          </ul>
        </nav>
      </PageContainer>
    </header>
  );
}
