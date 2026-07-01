import { getJson, putJson } from "./httpClient";
import type { UserStatus } from "../types/realtime";

interface ChangePresenceStatusRequest {
  status: UserStatus;
}

interface UserPresencePreferencesResponse {
  preferredStatus?: UserStatus | null;
}

export async function fetchPresencePreferences(accessToken: string): Promise<UserStatus | null> {
  const response = await getJson<UserPresencePreferencesResponse>("/api/presence/preferences", {
    accessToken,
  });

  return response.preferredStatus ?? null;
}

export async function changePresenceStatus(status: UserStatus, accessToken: string): Promise<void> {
  await putJson<unknown, ChangePresenceStatusRequest>(
    "/api/presence/status",
    { status },
    { accessToken },
  );
}
