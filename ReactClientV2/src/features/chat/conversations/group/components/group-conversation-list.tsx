import { UsersRound } from 'lucide-react';
import { ZodError } from 'zod';

import { Button } from '@/components/ui/button';

import { useGroups } from '../api/get-groups';

function getErrorMessage(error: unknown) {
  if (error instanceof ZodError) {
    return error.issues[0]?.message ?? error.message;
  }

  return error instanceof Error ? error.message : 'An unexpected error occurred.';
}

function GroupConversationListSkeleton() {
  return (
    <div className="grid gap-1" role="status" aria-label="Loading group conversations">
      {[0, 1, 2].map((item) => (
        <div className="flex items-center gap-3 px-2 py-3" key={item} aria-hidden="true">
          <span className="size-11 animate-pulse rounded-full bg-slate-200" />
          <span className="h-4 w-32 animate-pulse rounded-full bg-slate-200" />
        </div>
      ))}
    </div>
  );
}

export function GroupConversationList() {
  const { data, error, isError, isFetching, isPending, refetch } = useGroups();

  if (isPending) {
    return <GroupConversationListSkeleton />;
  }

  if (isError) {
    return (
      <div
        className="grid min-h-40 place-items-center border-t border-slate-200 px-4 py-8 text-center"
        role="alert"
      >
        <div className="grid max-w-56 justify-items-center gap-4">
          <div className="grid gap-2">
            <h2 className="m-0 font-display text-base font-semibold text-slate-900">
              Group conversations unavailable
            </h2>
            <p className="m-0 text-sm leading-6 text-slate-500">
              We could not load your group conversations.
            </p>
            <p className="m-0 break-words text-xs leading-5 text-rose-700">
              {getErrorMessage(error)}
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

  if (!data.groupConversations.length) {
    return (
      <div className="grid min-h-40 place-items-center border-t border-slate-200 px-4 py-8 text-center">
        <div className="grid max-w-56 justify-items-center gap-3">
          <span className="grid size-11 place-items-center rounded-full bg-violet-50 text-violet-700">
            <UsersRound className="size-5" aria-hidden="true" />
          </span>
          <div className="grid gap-3">
            <h2 className="m-0 font-display text-base font-semibold text-slate-900">
              No group conversations yet
            </h2>
            <p className="m-0 text-sm leading-6 text-slate-500">
              Your group conversations will appear here.
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <ul className="m-0 list-none divide-y divide-slate-200 p-0">
      {data.groupConversations.map((conversation) => (
        <li className="flex min-w-0 items-center gap-3 px-2 py-3" key={conversation.conversationId}>
          <span
            className="grid size-11 shrink-0 place-items-center rounded-full bg-violet-100 text-violet-800"
            aria-hidden="true"
          >
            <UsersRound className="size-5" />
          </span>
          <p className="m-0 min-w-0 truncate text-sm font-semibold text-slate-900">
            {conversation.name}
          </p>
        </li>
      ))}
    </ul>
  );
}
