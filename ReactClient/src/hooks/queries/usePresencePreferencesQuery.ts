import { useQuery } from "@tanstack/react-query";
import { fetchPresencePreferences } from "../../api/presenceApi";

export function usePresencePreferencesQuery(accessToken: string | null) {
  return useQuery({
    queryKey: ["presencePreferences"],
    queryFn: () => fetchPresencePreferences(accessToken!),
    enabled: Boolean(accessToken),
    staleTime: Infinity,
  });
}
