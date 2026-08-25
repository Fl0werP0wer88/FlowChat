import { X } from 'lucide-react';

import type { UserProfile } from '@/features/chat/user-profiles/api/search-user-profiles';
import { UserProfileAvatar } from '@/features/chat/user-profiles/components/user-search-results/user-profile-avatar';
import { getUserProfileDisplayName } from '@/features/chat/user-profiles/components/user-search-results/user-profile-display';

interface SelectedUsersProps {
  users: UserProfile[];
  disabled: boolean;
  onRemove: (userProfileId: string) => void;
}

export function SelectedUsers({ users, disabled, onRemove }: SelectedUsersProps) {
  if (!users.length) return null;

  return (
    <section className="grid gap-2.5" aria-labelledby="selected-members-heading">
      <div className="flex items-center justify-between gap-3">
        <h2
          className="m-0 text-xs font-extrabold uppercase tracking-[0.12em] text-slate-600"
          id="selected-members-heading"
        >
          Selected members
        </h2>
        <span className="text-xs font-semibold text-slate-500">{users.length}</span>
      </div>
      <ul className="m-0 flex list-none flex-wrap gap-2 p-0">
        {users.map((userProfile) => {
          const displayName = getUserProfileDisplayName(userProfile);

          return (
            <li
              className="flex min-w-0 items-center gap-2 rounded-full border border-slate-200 bg-white py-1 pr-1 pl-1.5"
              key={userProfile.id}
            >
              <UserProfileAvatar userProfile={userProfile} size="small" />
              <span className="max-w-28 truncate text-xs font-semibold text-slate-800">
                {displayName}
              </span>
              <button
                className="grid size-7 shrink-0 place-items-center rounded-full text-slate-500 transition hover:bg-rose-50 hover:text-rose-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 disabled:opacity-50"
                type="button"
                disabled={disabled}
                aria-label={`Remove ${displayName}`}
                onClick={() => onRemove(userProfile.id)}
              >
                <X className="size-3.5" aria-hidden="true" />
              </button>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
