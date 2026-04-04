const apiBaseUrl = (
  import.meta.env.VITE_GATEWAY_API_URL
    ?? import.meta.env.VITE_AUTH_API_URL
    ?? "https://localhost:7270"
).replace(/\/+$/, "");

type JsonRecord = Record<string, unknown>;

function parseJsonSafe(value: string): unknown {
  try {
    return JSON.parse(value);
  } catch {
    return value;
  }
}

function isJsonRecord(value: unknown): value is JsonRecord {
  return typeof value === "object" && value !== null;
}

function resolveErrorMessage(payload: unknown, statusCode: number): string {
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

  return `Request failed with status ${statusCode}.`;
}

export async function postJson<TResponse, TRequest extends object>(
  path: string,
  payload: TRequest,
): Promise<TResponse> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  const rawText = await response.text();
  const parsedPayload = rawText.length > 0 ? parseJsonSafe(rawText) : null;

  if (!response.ok) {
    throw new Error(resolveErrorMessage(parsedPayload, response.status));
  }

  return parsedPayload as TResponse;
}

export async function postForm<TResponse>(
  path: string,
  payload: Record<string, string>,
): Promise<TResponse> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/x-www-form-urlencoded",
    },
    body: new URLSearchParams(payload),
  });

  const rawText = await response.text();
  const parsedPayload = rawText.length > 0 ? parseJsonSafe(rawText) : null;

  if (!response.ok) {
    throw new Error(resolveErrorMessage(parsedPayload, response.status));
  }

  return parsedPayload as TResponse;
}
