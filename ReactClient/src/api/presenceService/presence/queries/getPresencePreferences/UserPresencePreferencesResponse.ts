import type { UserStatus } from "../../../../../types/realtime";

export interface UserPresencePreferencesResponse {
  preferredStatus?: UserStatus | null;
}
