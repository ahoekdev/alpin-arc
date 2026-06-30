export function readCookie(name: string): string | undefined {
  if (typeof document === "undefined") {
    return undefined;
  }

  return document.cookie
    .split("; ")
    .find((part) => part.startsWith(`${name}=`))
    ?.split("=")
    .slice(1)
    .join("=");
}

export async function ensureCsrfToken(): Promise<string | undefined> {
  let token = readCookie("XSRF-TOKEN");

  if (!token) {
    await fetch("/api/auth/csrf", {
      method: "GET",
      credentials: "same-origin",
    });
    token = readCookie("XSRF-TOKEN");
  }

  return token ? decodeURIComponent(token) : undefined;
}
