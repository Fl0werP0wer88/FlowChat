import { useMemo } from "react";
import type { Contact } from "../../../types/contacts";

const defaultContacts: Contact[] = [
  { id: 1, displayName: "Anna Kowalska", status: "online" },
  { id: 2, displayName: "Michal Nowak", status: "away" },
  { id: 3, displayName: "Joanna Wisniewska", status: "offline" },
  { id: 4, displayName: "Krzysztof Lewandowski", status: "online" }
];

export function useContacts(): Contact[] {
  return useMemo(() => defaultContacts, []);
}
