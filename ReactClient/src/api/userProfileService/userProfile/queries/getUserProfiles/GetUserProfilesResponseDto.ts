import type { UserProfileSearchDto } from "../searchUsers/UserProfileSearchDto";

export interface GetUserProfilesResponseDto {
  userProfiles: UserProfileSearchDto[];
}
