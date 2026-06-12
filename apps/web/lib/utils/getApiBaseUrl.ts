export default function getApiBaseUrl(): string {
  const apiBaseUrl = process.env.API_BASE_URL;

  if (!apiBaseUrl) {
    throw new Error("API_BASE_URL is not defined in environment variables");
  }

  return apiBaseUrl;
}
