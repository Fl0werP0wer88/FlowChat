import { getJson } from "../../api/httpClient";

interface SearchUsersRequest {
  firstName?: string;
  lastName?: string;
  organization?: string;
}

interface UserProfileSearchDto {
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
  userProfiles?: UserProfileSearchDto[];
  UserProfiles?: UserProfileSearchDto[];
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

function buildSearchUserDisplayName(dto: UserProfileSearchDto): string {
  const firstName = (dto.firstName ?? dto.FirstName ?? "").trim();
  const lastName = (dto.lastName ?? dto.LastName ?? "").trim();
  const displayName = `${firstName} ${lastName}`.trim();

  return displayName || "Nieznany uzytkownik";
}

function mapSearchUserResult(dto: UserProfileSearchDto): SearchUserResult {
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
    `/api/userprofiles/search${buildQueryString(normalizedCriteria)}`,
    {
      accessToken,
      signal,
    },
  );

  const results = response.userProfiles ?? response.UserProfiles ?? [];
  return results.map(mapSearchUserResult);
}
