import { getJson, putJson } from "../httpClient";
import type { UserStatus } from "../../types/realtime";
import type { ChangePresenceStatusRequest } from "./presence/commands/changePresenceStatus/ChangePresenceStatusRequest";
import type { UserPresencePreferencesResponse } from "./presence/queries/getPresencePreferences/UserPresencePreferencesResponse";

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
