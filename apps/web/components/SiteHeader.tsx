import Link from "next/link";
import { PageContainer } from "./PageContainer";

export default function SiteHeader() {
  return (
    <header className="h-16 flex items-center border-b border-gray-200">
      <PageContainer>
        <nav>
          <ul className="flex gap-4">
            <li>
              <Link href="/">Home</Link>
            </li>
            <li>
              <Link href="/explore">Explore</Link>
            </li>
            <li>
              <Link href="/tours">Tours</Link>
            </li>
          </ul>
        </nav>
      </PageContainer>
    </header>
  );
}
