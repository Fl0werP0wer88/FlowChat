import axios, { type AxiosRequestConfig } from "axios";

const apiBaseUrl = (
  import.meta.env.VITE_GATEWAY_API_URL
    ?? "https://localhost:7270"
).replace(/\/+$/, "");

type JsonRecord = Record<string, unknown>;
interface RequestOptions {
  accessToken?: string;
  signal?: AbortSignal;
}

const httpClient = axios.create({
  baseURL: apiBaseUrl,
  withCredentials: true,
});

function isJsonRecord(value: unknown): value is JsonRecord {
  return typeof value === "object" && value !== null;
}

function resolveErrorMessage(payload: unknown, statusCode?: number): string {
  if (typeof payload === "string" && payload.trim().length > 0) {
    return payload;
  }

  if (isJsonRecord(payload)) {
    const errorDescription = payload.error_description;
    if (typeof errorDescription === "string" && errorDescription.trim().length > 0) {
      return errorDescription;
    }

    const message = payload.message;
    if (typeof message === "string" && message.trim().length > 0) {
      return message;
    }

    const detail = payload.detail;
    if (typeof detail === "string" && detail.trim().length > 0) {
      return detail;
    }

    const title = payload.title;
    if (typeof title === "string" && title.trim().length > 0) {
      return title;
    }
  }

  return `Request failed with status ${statusCode ?? "unknown"}.`;
}

function createHeaders(contentType: string | null, accessToken?: string): Record<string, string> {
  const headers: Record<string, string> = {};

  if (contentType) {
    headers["Content-Type"] = contentType;
  }

  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  return headers;
}

async function request<TResponse>(
  config: AxiosRequestConfig,
  options: RequestOptions = {},
): Promise<TResponse> {
  try {
    const response = await httpClient.request<TResponse>({
      ...config,
      headers: {
        ...createHeaders(null, options.accessToken),
        ...config.headers,
      },
      signal: options.signal,
    });

    return (response.data === "" ? null : response.data) as TResponse;
  } catch (error) {
    if (axios.isAxiosError(error)) {
      throw new Error(resolveErrorMessage(error.response?.data, error.response?.status));
    }

    throw error;
  }
}

export async function postJson<TResponse, TRequest extends object>(
  path: string,
  payload: TRequest,
  options: RequestOptions = {},
): Promise<TResponse> {
  return request<TResponse>({
    method: "POST",
    url: path,
    data: payload,
    headers: createHeaders("application/json"),
  }, options);
}

export async function putJson<TResponse, TRequest extends object>(
  path: string,
  payload: TRequest,
  options: RequestOptions = {},
): Promise<TResponse> {
  return request<TResponse>({
    method: "PUT",
    url: path,
    data: payload,
    headers: createHeaders("application/json"),
  }, options);
}

export async function postForm<TResponse>(
  path: string,
  payload: Record<string, string>,
  options: RequestOptions = {},
): Promise<TResponse> {
  return request<TResponse>({
    method: "POST",
    url: path,
    data: new URLSearchParams(payload),
    headers: createHeaders("application/x-www-form-urlencoded"),
  }, options);
}

export async function getJson<TResponse>(
  path: string,
  options: RequestOptions = {},
): Promise<TResponse> {
  return request<TResponse>({
    method: "GET",
    url: path,
  }, options);
}
