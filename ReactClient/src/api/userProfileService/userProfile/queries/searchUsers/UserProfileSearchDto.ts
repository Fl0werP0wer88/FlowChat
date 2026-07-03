export interface UserProfileSearchDto {
  id: string;
  friendlyUserId: string;
  firstName?: string | null;
  lastName?: string | null;
  organization?: string | null;
  avatarUrl?: string | null;
}
