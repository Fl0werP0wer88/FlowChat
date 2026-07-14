export interface SearchUserResult {
  userProfileId: string;
  friendlyUserId: string;
  displayName: string;
  firstName: string | null;
  lastName: string | null;
  organization: string | null;
}

export interface SearchUsersCriteria {
  firstName: string;
  lastName: string;
  organization: string;
}
