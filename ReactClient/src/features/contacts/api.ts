import { getJson, postJson } from "../../api/httpClient";
import type { Contact } from "../../types/contacts";

interface ContactDto {
  id?: string;
  Id?: string;
  displayName?: string;
  DisplayName?: string;
}

interface GetContactsResponseDto {
  contacts?: ContactDto[];
  Contacts?: ContactDto[];
}

interface AddContactPayload {
  ownerUserId: string;
  friendlyUserId?: string;
  email?: string;
}

interface AddContactResponseDto {
  contactId?: string;
  ContactId?: string;
}

function resolveContacts(response: GetContactsResponseDto): ContactDto[] {
  return response.contacts ?? response.Contacts ?? [];
}

function mapContact(dto: ContactDto): Contact {
  return {
    id: dto.id ?? dto.Id ?? crypto.randomUUID(),
    displayName: dto.displayName ?? dto.DisplayName ?? "Nowy kontakt",
    status: "offline",
  };
}

export function isEmailLookup(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}

export async function fetchContacts(ownerUserId: string, accessToken: string): Promise<Contact[]> {
  const response = await getJson<GetContactsResponseDto>(`/api/contacts/${ownerUserId}`, {
    accessToken,
  });

  return resolveContacts(response).map(mapContact);
}

export async function addContact(
  ownerUserId: string,
  lookupValue: string,
  accessToken: string,
): Promise<string | null> {
  const trimmedLookupValue = lookupValue.trim();
  const payload: AddContactPayload = {
    ownerUserId,
    ...(isEmailLookup(trimmedLookupValue)
      ? { email: trimmedLookupValue }
      : { friendlyUserId: trimmedLookupValue }),
  };

  const response = await postJson<AddContactResponseDto, AddContactPayload>("/api/contacts", payload, {
    accessToken,
  });

  return response.contactId ?? response.ContactId ?? null;
}
