import { useQuery } from "@tanstack/react-query";
import { fetchContacts } from "../../api/gatewayApi";

export function useContactsQuery(accessToken: string, enabled: boolean) {
  return useQuery({
    queryKey: ["contacts"],
    queryFn: () => fetchContacts(accessToken),
    enabled,
  });
}
