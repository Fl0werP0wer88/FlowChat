import axios from 'axios';

type JsonObject = Record<string, unknown>;

export class ApiError extends Error {
  public readonly status: number | null;

  public constructor(message: string, status: number | null = null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

function isObject(value: unknown): value is JsonObject {
  return typeof value === 'object' && value !== null;
}

function firstValidationError(payload: JsonObject): string | null {
  const errors = payload.errors;
  if (!isObject(errors)) return null;

  for (const value of Object.values(errors)) {
    if (Array.isArray(value) && typeof value[0] === 'string') return value[0];
    if (typeof value === 'string') return value;
  }

  return null;
}

function resolveMessage(payload: unknown, fallback: string): string {
  if (typeof payload === 'string' && payload.trim()) return payload;
  if (!isObject(payload)) return fallback;

  const validationError = firstValidationError(payload);
  if (validationError) return validationError;

  for (const key of ['error_description', 'detail', 'message', 'title'] as const) {
    const value = payload[key];
    if (typeof value === 'string' && value.trim()) return value;
  }

  return fallback;
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;

  if (axios.isAxiosError(error)) {
    const status = error.response?.status ?? null;
    const fallback = status
      ? `Request failed with status ${status}.`
      : 'Unable to reach the server.';
    return new ApiError(resolveMessage(error.response?.data, fallback), status);
  }

  if (error instanceof Error) return new ApiError(error.message);
  return new ApiError('An unexpected error occurred.');
}
