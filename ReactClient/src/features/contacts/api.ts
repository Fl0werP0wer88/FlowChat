import { getJson, postJson } from "../../api/httpClient";
import type { Contact } from "../../types/contacts";

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

interface SearchUsersRequest {
  firstName?: string;
  lastName?: string;
  organization?: string;
}

interface UserProfileProjectionDto {
  userProfileId?: string;
  UserProfileId?: string;
  friendlyUserId?: string;
  FriendlyUserId?: string;
  firstName?: string | null;
  FirstName?: string | null;
  lastName?: string | null;
  LastName?: string | null;
  organization?: string | null;
  Organization?: string | null;
}

interface SearchUsersResponseDto {
  userProfiles?: UserProfileProjectionDto[];
  UserProfiles?: UserProfileProjectionDto[];
}

export interface SearchUsersCriteria {
  firstName: string;
  lastName: string;
  organization: string;
}

export interface SearchUserResult {
  userProfileId: string;
  friendlyUserId: string;
  displayName: string;
  firstName: string | null;
  lastName: string | null;
  organization: string | null;
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
    status: "Invisible",
    conversationId: dto.conversationId ?? dto.ConversationId ?? null,
  };
}

function buildQueryString(parameters: SearchUsersRequest): string {
  const searchParams = new URLSearchParams();

  if (parameters.firstName) {
    searchParams.set("firstName", parameters.firstName);
  }

  if (parameters.lastName) {
    searchParams.set("lastName", parameters.lastName);
  }

  if (parameters.organization) {
    searchParams.set("organization", parameters.organization);
  }

  const serialized = searchParams.toString();
  return serialized.length > 0 ? `?${serialized}` : "";
}

function buildSearchUserDisplayName(dto: UserProfileProjectionDto): string {
  const firstName = (dto.firstName ?? dto.FirstName ?? "").trim();
  const lastName = (dto.lastName ?? dto.LastName ?? "").trim();
  const displayName = `${firstName} ${lastName}`.trim();

  return displayName || "Nieznany uzytkownik";
}

function mapSearchUserResult(dto: UserProfileProjectionDto): SearchUserResult {
  return {
    userProfileId: dto.userProfileId ?? dto.UserProfileId ?? crypto.randomUUID(),
    friendlyUserId: dto.friendlyUserId ?? dto.FriendlyUserId ?? "",
    displayName: buildSearchUserDisplayName(dto),
    firstName: dto.firstName ?? dto.FirstName ?? null,
    lastName: dto.lastName ?? dto.LastName ?? null,
    organization: dto.organization ?? dto.Organization ?? null,
  };
}

export function isEmailLookup(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}

export async function fetchContacts(accessToken: string): Promise<Contact[]> {
  const response = await getJson<GetContactsResponseDto>("/api/aggregate/contacts", {
    accessToken,
  });

  return resolveContacts(response).map(mapContact);
}

export async function addContact(
  lookupValue: string,
  accessToken: string,
): Promise<string | null> {
  const trimmedLookupValue = lookupValue.trim();
  const payload: AddContactPayload = {
    id: crypto.randomUUID(),
    ...(isEmailLookup(trimmedLookupValue)
      ? { email: trimmedLookupValue }
      : { friendlyUserId: trimmedLookupValue }),
  };

  const response = await postJson<AddContactResponseDto, AddContactPayload>("/api/contacts", payload, {
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

  const response = await postJson<AddContactResponseDto, typeof payload>("/api/contacts", payload, {
    accessToken,
  });

  return response.contactId ?? response.ContactId ?? null;
}

export async function searchUsers(
  criteria: SearchUsersCriteria,
  accessToken: string,
  signal?: AbortSignal,
): Promise<SearchUserResult[]> {
  const normalizedCriteria: SearchUsersRequest = {
    firstName: criteria.firstName.trim() || undefined,
    lastName: criteria.lastName.trim() || undefined,
    organization: criteria.organization.trim() || undefined,
  };

  const response = await getJson<SearchUsersResponseDto>(
    `/api/userprofiles/projections/socialgraph/search${buildQueryString(normalizedCriteria)}`,
    {
      accessToken,
      signal,
    },
  );

  const results = response.userProfiles ?? response.UserProfiles ?? [];
  return results.map(mapSearchUserResult);
}
