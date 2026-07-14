import type { SearchUserResult } from "../../types/users";
import type { UserProfileSearchDto } from "./userProfile/queries/searchUsers/UserProfileSearchDto";

function buildSearchUserDisplayName(dto: UserProfileSearchDto): string {
  const firstName = (dto.firstName ?? "").trim();
  const lastName = (dto.lastName ?? "").trim();
  const displayName = `${firstName} ${lastName}`.trim();

  return displayName || "Nieznany uzytkownik";
}

export function mapSearchUserResult(dto: UserProfileSearchDto): SearchUserResult {
  return {
    userProfileId: dto.id,
    friendlyUserId: dto.friendlyUserId,
    displayName: buildSearchUserDisplayName(dto),
    firstName: dto.firstName ?? null,
    lastName: dto.lastName ?? null,
    organization: dto.organization ?? null,
  };
}
