import type { UserStatus } from "../../../../../types/realtime";

export interface ChangePresenceStatusRequest {
  status: UserStatus;
}
