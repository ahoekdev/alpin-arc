import Link from "next/link";

export default function SiteHeader() {
  return (
    <header className="container mx-auto px-4 h-16 flex items-center justify-between">
      <nav>
        <ul className="flex gap-4">
          <li>
            <Link href="/">Home</Link>
          </li>
          <li>
            <Link href="/lodges">Lodges</Link>
          </li>
          <li>
            <Link href="/stages">Stages</Link>
          </li>
        </ul>
      </nav>
    </header>
  );
}
