import { getJson, putJson } from "../../api/httpClient";
import type { Contact } from "../../types/contacts";
import { isEmail } from "../../utils/stringUtils";

interface ContactDto {
  id?: string;
  Id?: string;
  contactUserId?: string;
  ContactUserId?: string;
  displayName?: string;
  DisplayName?: string;
  email?: string | null;
  Email?: string | null;
  conversationId?: string | null;
  ConversationId?: string | null;
  status?: Contact["status"];
  Status?: Contact["status"];
}

interface GetContactsResponseDto {
  contacts?: ContactDto[];
  Contacts?: ContactDto[];
}

interface AddContactPayload {
  id: string;
  userId?: string;
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
  const userId = dto.contactUserId ?? dto.ContactUserId ?? dto.id ?? dto.Id ?? crypto.randomUUID();

  return {
    id: dto.id ?? dto.Id ?? crypto.randomUUID(),
    userId,
    displayName: dto.displayName ?? dto.DisplayName ?? "Nowy kontakt",
    email: dto.email ?? dto.Email ?? null,
    status: dto.status ?? dto.Status ?? "Invisible",
    conversationId: dto.conversationId ?? dto.ConversationId ?? null,
  };
}

export async function fetchContacts(accessToken: string): Promise<Contact[]> {
  const response = await getJson<GetContactsResponseDto>("/api/aggregate/contacts", {
    accessToken,
  });

  return resolveContacts(response).map(mapContact);
}

export async function addContact(
  emailOrFriendlyId: string,
  accessToken: string,
): Promise<string | null> {
  const trimmedEmailOrFriendlyId = emailOrFriendlyId.trim();
  const payload: AddContactPayload = {
    id: crypto.randomUUID(),
    ...(isEmail(trimmedEmailOrFriendlyId)
      ? { email: trimmedEmailOrFriendlyId }
      : { friendlyUserId: trimmedEmailOrFriendlyId }),
  };

  const response = await putJson<AddContactResponseDto, AddContactPayload>("/api/contacts", payload, {
    accessToken,
  });

  return response.contactId ?? response.ContactId ?? null;
}

export async function addContactByUserId(
  userId: string,
  accessToken: string,
): Promise<string | null> {
  const payload: AddContactPayload = {
    id: crypto.randomUUID(),
    userId,
  };

  const response = await putJson<AddContactResponseDto, typeof payload>("/api/contacts", payload, {
    accessToken,
  });

  return response.contactId ?? response.ContactId ?? null;
}
