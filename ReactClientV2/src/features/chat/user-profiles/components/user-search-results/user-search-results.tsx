import { Check, LoaderCircle, UserPlus } from 'lucide-react';
import { useEffect, useRef, type RefObject } from 'react';

import { Button } from '@/components/ui/button';
import { toApiError } from '@/lib/api-error';
import { cn } from '@/utils/cn';

import {
  type SearchUserProfilesInput,
  type UserProfile,
  useSearchUserProfiles,
} from '../../api/search-user-profiles';

import { UserProfileAvatar } from './user-profile-avatar';
import { getUserProfileDisplayName } from './user-profile-display';
import { UserSearchLoading, UserSearchStatus } from './user-search-status';

interface UserSearchResultsProps {
  criteria: SearchUserProfilesInput;
  currentUserId?: string;
  selectedUserIds: ReadonlySet<string>;
  mode: 'single' | 'multiple';
  disabled: boolean;
  processingUserId?: string;
  scrollContainerRef: RefObject<HTMLDivElement | null>;
  onSelect: (userProfile: UserProfile) => void;
}

export function UserSearchResults({
  criteria,
  currentUserId,
  selectedUserIds,
  mode,
  disabled,
  processingUserId,
  scrollContainerRef,
  onSelect,
}: UserSearchResultsProps) {
  const {
    data,
    error,
    fetchNextPage,
    hasNextPage,
    isError,
    isFetchNextPageError,
    isFetching,
    isFetchingNextPage,
    isPending,
    refetch,
  } = useSearchUserProfiles({ input: criteria });
  const loadMoreRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const target = loadMoreRef.current;
    if (
      !target ||
      !hasNextPage ||
      isFetchingNextPage ||
      isFetchNextPageError ||
      typeof IntersectionObserver === 'undefined'
    ) {
      return;
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry?.isIntersecting) {
          void fetchNextPage();
        }
      },
      {
        root: scrollContainerRef.current,
        rootMargin: '0px 0px 160px 0px',
      },
    );

    observer.observe(target);
    return () => observer.disconnect();
  }, [scrollContainerRef, fetchNextPage, hasNextPage, isFetchNextPageError, isFetchingNextPage]);

  if (isPending) {
    return <UserSearchLoading />;
  }

  if (isError && !data) {
    return (
      <div className="grid min-h-32 place-items-center px-4 py-8 text-center" role="alert">
        <div className="grid max-w-60 justify-items-center gap-3">
          <div className="grid gap-1.5">
            <p className="m-0 text-sm font-semibold text-slate-900">Search unavailable</p>
            <p className="m-0 break-words text-xs leading-5 text-rose-700">
              {toApiError(error).message}
            </p>
          </div>
          <Button
            className="min-h-9 px-4"
            type="button"
            variant="secondary"
            isLoading={isFetching}
            onClick={() => void refetch()}
          >
            Try again
          </Button>
        </div>
      </div>
    );
  }

  const userProfiles = data.pages
    .flatMap((page) => page.items)
    .filter((userProfile) => userProfile.id !== currentUserId);

  if (!userProfiles.length && !hasNextPage) {
    return <UserSearchStatus>No users match these search criteria.</UserSearchStatus>;
  }

  return (
    <>
      {userProfiles.length ? (
        <ul
          className="m-0 list-none divide-y divide-slate-200 p-0"
          aria-label="User search results"
        >
          {userProfiles.map((userProfile) => {
            const displayName = getUserProfileDisplayName(userProfile);
            const isSelected = selectedUserIds.has(userProfile.id);
            const isProcessing = processingUserId === userProfile.id;

            return (
              <li key={userProfile.id}>
                <button
                  className={cn(
                    'flex min-h-16 w-full items-center gap-3 px-1 py-2.5 text-left transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-blue-600 disabled:cursor-not-allowed disabled:opacity-60',
                    isSelected ? 'bg-blue-50/80' : 'hover:bg-white',
                  )}
                  type="button"
                  disabled={disabled}
                  aria-pressed={mode === 'multiple' ? isSelected : undefined}
                  onClick={() => onSelect(userProfile)}
                >
                  <UserProfileAvatar userProfile={userProfile} />
                  <span className="grid min-w-0 flex-1 gap-0.5">
                    <strong className="truncate text-sm font-semibold text-slate-900">
                      {displayName}
                    </strong>
                    {displayName !== `@${userProfile.friendlyUserId}` ? (
                      <span className="truncate text-xs text-slate-500">
                        @{userProfile.friendlyUserId}
                      </span>
                    ) : null}
                    {userProfile.organization ? (
                      <span className="truncate text-xs text-slate-500">
                        {userProfile.organization}
                      </span>
                    ) : null}
                  </span>
                  <span
                    className={cn(
                      'grid size-8 shrink-0 place-items-center rounded-full',
                      isSelected ? 'bg-blue-700 text-white' : 'bg-blue-50 text-blue-700',
                    )}
                    aria-hidden="true"
                  >
                    {isProcessing ? (
                      <LoaderCircle className="size-4 animate-spin" />
                    ) : isSelected ? (
                      <Check className="size-4" />
                    ) : (
                      <UserPlus className="size-4" />
                    )}
                  </span>
                </button>
              </li>
            );
          })}
        </ul>
      ) : null}

      {isFetchNextPageError ? (
        <div className="grid justify-items-center gap-2 py-5 text-center" role="alert">
          <p className="m-0 break-words text-xs leading-5 text-rose-700">
            {toApiError(error).message}
          </p>
          <Button
            className="min-h-9 px-4"
            type="button"
            variant="secondary"
            isLoading={isFetchingNextPage}
            onClick={() => void fetchNextPage()}
          >
            Try again
          </Button>
        </div>
      ) : hasNextPage ? (
        <div ref={loadMoreRef}>
          {isFetchingNextPage ? (
            <div
              className="flex items-center justify-center gap-2 py-5 text-xs text-slate-500"
              role="status"
            >
              <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />
              Loading more users...
            </div>
          ) : (
            <div className="h-px" aria-hidden="true" />
          )}
        </div>
      ) : null}
    </>
  );
}
