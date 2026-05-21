import { getJson } from "../../api/httpClient";

interface SearchUsersRequest {
  firstName?: string;
  lastName?: string;
  organization?: string;
}

interface UserProfileSearchDto {
  id: string;
  friendlyUserId: string;
  firstName?: string | null;
  lastName?: string | null;
  organization?: string | null;
  avatarUrl?: string | null;
}

interface SearchUsersResponseDto {
  userProfiles: UserProfileSearchDto[];
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
  const firstName = (dto.firstName ?? "").trim();
  const lastName = (dto.lastName ?? "").trim();
  const displayName = `${firstName} ${lastName}`.trim();

  return displayName || "Nieznany uzytkownik";
}

function mapSearchUserResult(dto: UserProfileSearchDto): SearchUserResult {
  return {
    userProfileId: dto.id,
    friendlyUserId: dto.friendlyUserId,
    displayName: buildSearchUserDisplayName(dto),
    firstName: dto.firstName ?? null,
    lastName: dto.lastName ?? null,
    organization: dto.organization ?? null,
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

  return response.userProfiles.map(mapSearchUserResult);
}
