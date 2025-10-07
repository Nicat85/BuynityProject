import axios from "axios";
import { env } from "@/lib/env";

const api = axios.create({
  baseURL: `${env.NEXT_PUBLIC_API_BASE_URL}/api`,
  withCredentials: true,
});

let accessToken: string | null = null;
let refreshToken: string | null = null;

export function setTokens(tokens: { accessToken: string; refreshToken: string } | null) {
  accessToken = tokens?.accessToken ?? null;
  refreshToken = tokens?.refreshToken ?? null;
}

api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers = config.headers ?? {};
    (config.headers as any)["Authorization"] = `Bearer ${accessToken}`;
  }
  return config;
});

let isRefreshing = false;
let pendingRequests: Array<(token: string | null) => void> = [];

api.interceptors.response.use(
  (r) => r,
  async (error) => {
    const original = error.config;
    if (error.response?.status === 401 && !original._retry && refreshToken) {
      original._retry = true;
      try {
        if (!isRefreshing) {
          isRefreshing = true;
          const resp = await axios.post(
            `${env.NEXT_PUBLIC_API_BASE_URL}/api/auth/refresh-token`,
            { refreshToken },
            { headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined }
          );
          const data = resp.data?.data || resp.data?.Data || resp.data; 
          const newAccess = data.accessToken || data.AccessToken;
          const newRefresh = data.refreshToken || data.RefreshToken || refreshToken;
          accessToken = newAccess;
          refreshToken = newRefresh;
          pendingRequests.forEach((cb) => cb(newAccess));
          pendingRequests = [];
        }

        return new Promise((resolve, reject) => {
          pendingRequests.push((token) => {
            if (token) {
              original.headers = original.headers ?? {};
              original.headers["Authorization"] = `Bearer ${token}`;
            }
            axios(original).then(resolve).catch(reject);
          });
        });
      } catch (e) {
        pendingRequests = [];
        isRefreshing = false;
        return Promise.reject(e);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export default api;
