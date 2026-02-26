const gatewayBaseUrl = (import.meta.env.VITE_GATEWAY_API_URL ?? "https://localhost:7305").replace(/\/+$/, "");

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
    const message = payload.message;
    if (typeof message === "string" && message.trim().length > 0) {
      return message;
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
  const response = await fetch(`${gatewayBaseUrl}${path}`, {
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
