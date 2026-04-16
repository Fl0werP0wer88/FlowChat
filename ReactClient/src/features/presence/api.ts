import { putJson } from "../../api/httpClient";
import type { UserStatus } from "../../types/realtime";

interface ChangePresenceStatusRequest {
  status: UserStatus;
}

export async function changePresenceStatus(status: UserStatus, accessToken: string): Promise<void> {
  await putJson<unknown, ChangePresenceStatusRequest>(
    "/api/presence/status",
    { status },
    { accessToken },
  );
}
