import axios, {
  type AxiosError,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios';

import { env } from '@/config/env';
import { getAuthState } from '@/stores/auth-store';

import { toApiError } from './api-error';

declare module 'axios' {
  export interface AxiosRequestConfig {
    skipAuth?: boolean;
    skipAuthRefresh?: boolean;
    authRetryAttempted?: boolean;
  }
}

type RefreshHandler = () => Promise<string | null>;

let refreshHandler: RefreshHandler | null = null;

export const api = axios.create({
  baseURL: env.gatewayApiUrl,
  withCredentials: true,
  headers: {
    Accept: 'application/json',
  },
});

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (!config.skipAuth) {
    const token = getAuthState().session?.accessToken;
    if (token) config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const config = error.config as AxiosRequestConfig | undefined;
    const handler = refreshHandler;
    const canRefresh =
      error.response?.status === 401 &&
      config &&
      !config.skipAuthRefresh &&
      !config.authRetryAttempted &&
      handler;

    if (canRefresh) {
      config.authRetryAttempted = true;
      const accessToken = await handler();

      if (accessToken) {
        config.headers = {
          ...config.headers,
          Authorization: `Bearer ${accessToken}`,
        };

        return api.request(config);
      }
    }

    return Promise.reject(toApiError(error));
  },
);

export function configureAuthRefresh(handler: RefreshHandler) {
  refreshHandler = handler;
}
