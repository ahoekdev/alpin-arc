import getApiBaseUrl from "../utils/getApiBaseUrl";

type Lodge = {
  id: number;
  name: string;
  createdAt: string;
};

export async function getLodges(): Promise<Lodge[]> {
  const response = await fetch(`${getApiBaseUrl()}/api/lodges`);

  if (!response.ok) {
    throw new Error(`Failed to fetch lodges: ${response.status}`);
  }

  return response.json();
}
