import { cn } from '@/utils/cn';

import type { UserProfile } from '../api/search-user-profile';

import { getUserProfileInitials } from './user-profile-display';

export function UserProfileAvatar({
  userProfile,
  size = 'regular',
}: {
  userProfile: UserProfile;
  size?: 'small' | 'regular';
}) {
  const sizeClasses = size === 'small' ? 'size-8 text-xs' : 'size-11 text-sm';

  return userProfile.avatarUrl ? (
    <img
      className={cn('shrink-0 rounded-full bg-slate-200 object-cover', sizeClasses)}
      src={userProfile.avatarUrl}
      alt=""
      loading="lazy"
      decoding="async"
    />
  ) : (
    <span
      className={cn(
        'grid shrink-0 place-items-center rounded-full bg-blue-100 font-bold text-blue-800',
        sizeClasses,
      )}
      aria-hidden="true"
    >
      {getUserProfileInitials(userProfile)}
    </span>
  );
}
