import type { UserProfile } from '../../api/search-user-profiles';

export function getUserProfileDisplayName(userProfile: UserProfile) {
  const name = [userProfile.firstName, userProfile.lastName]
    .map((value) => value?.trim())
    .filter(Boolean)
    .join(' ');

  return name || `@${userProfile.friendlyUserId}`;
}

export function getUserProfileInitials(userProfile: UserProfile) {
  const name = [userProfile.firstName, userProfile.lastName]
    .map((value) => value?.trim())
    .filter(Boolean);

  if (name.length) {
    return name
      .slice(0, 2)
      .map((value) => value?.[0])
      .join('')
      .toUpperCase();
  }

  return userProfile.friendlyUserId.slice(0, 2).toUpperCase() || '?';
}
