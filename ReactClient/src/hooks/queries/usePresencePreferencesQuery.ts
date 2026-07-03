import { useQuery } from "@tanstack/react-query";
import { fetchPresencePreferences } from "../../api/presenceService";

export function usePresencePreferencesQuery(accessToken: string | null) {
  return useQuery({
    queryKey: ["presencePreferences"],
    queryFn: () => fetchPresencePreferences(accessToken!),
    enabled: Boolean(accessToken),
    staleTime: Infinity,
  });
}
