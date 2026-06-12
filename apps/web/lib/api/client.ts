import createClient from "openapi-fetch";

import type { paths } from "./generated/schema";
import getApiBaseUrl from "../utils/getApiBaseUrl";

const apiClient = createClient<paths>({
  baseUrl: getApiBaseUrl(),
});

export default apiClient;
