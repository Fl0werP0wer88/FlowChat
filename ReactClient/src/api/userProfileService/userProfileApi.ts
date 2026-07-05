import { getJson, postJson } from "../httpClient";
import type { ConfirmEmailVerificationRequest } from "./userProfile/emailVerification/confirmEmailVerification/ConfirmEmailVerificationRequest";
import type { SearchUsersRequest } from "./userProfile/queries/searchUsers/SearchUsersRequest";
import type { GetUserProfileResponseDto } from "./userProfile/queries/getUserProfile/GetUserProfileResponseDto";
import type { GetUserProfilesResponseDto } from "./userProfile/queries/getUserProfiles/GetUserProfilesResponseDto";
import type { SearchUsersResponseDto } from "./userProfile/queries/searchUsers/SearchUsersResponseDto";
import type { UserProfileSearchDto } from "./userProfile/queries/searchUsers/UserProfileSearchDto";
import type { SearchUserResult, SearchUsersCriteria } from "../../types/users";

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

export async function getUserProfileById(
  userProfileId: string,
  accessToken: string,
  signal?: AbortSignal,
): Promise<SearchUserResult> {
  const response = await getJson<GetUserProfileResponseDto>(
    `/api/userprofiles/${encodeURIComponent(userProfileId)}`,
    {
      accessToken,
      signal,
    },
  );

  return mapSearchUserResult(response.userProfile);
}

export async function getUserProfilesByIds(
  userProfileIds: string[],
  accessToken: string,
  signal?: AbortSignal,
): Promise<SearchUserResult[]> {
  const searchParams = new URLSearchParams();
  userProfileIds.forEach((userProfileId) => {
    searchParams.append("userIds", userProfileId);
  });

  const response = await getJson<GetUserProfilesResponseDto>(
    `/api/userprofiles/by-ids?${searchParams.toString()}`,
    {
      accessToken,
      signal,
    },
  );

  return response.userProfiles.map(mapSearchUserResult);
}

export async function getUserProfileByEmail(
  email: string,
  accessToken: string,
  signal?: AbortSignal,
): Promise<SearchUserResult> {
  const response = await getJson<GetUserProfileResponseDto>(
    `/api/userprofiles/by-email?email=${encodeURIComponent(email)}`,
    {
      accessToken,
      signal,
    },
  );

  return mapSearchUserResult(response.userProfile);
}

export async function getUserProfileByFriendlyUserId(
  friendlyUserId: string,
  accessToken: string,
  signal?: AbortSignal,
): Promise<SearchUserResult> {
  const response = await getJson<GetUserProfileResponseDto>(
    `/api/userprofiles/by-friendly-id/${encodeURIComponent(friendlyUserId)}`,
    {
      accessToken,
      signal,
    },
  );

  return mapSearchUserResult(response.userProfile);
}

export async function confirmEmailVerification(token: string): Promise<void> {
  await postJson<unknown, ConfirmEmailVerificationRequest>("/api/userprofiles/email-verification/confirm", {
    token: token.trim(),
  });
}
