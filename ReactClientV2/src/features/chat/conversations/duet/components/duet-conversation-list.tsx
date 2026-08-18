import { MessageCircleMore } from 'lucide-react';
import { ZodError } from 'zod';

import { Button } from '@/components/ui/button';
import { cn } from '@/utils/cn';

import {
  type DuetConversationWithPresence,
  type PresenceStatus,
  useDuetsWithPresence,
} from '../api/get-duets-with-presence';

const presenceClasses: Record<PresenceStatus, string> = {
  Active: 'bg-emerald-500',
  AFK: 'bg-amber-400',
  Busy: 'bg-red-500',
  Invisible: 'bg-slate-400',
};

function getPartnerName(conversation: DuetConversationWithPresence) {
  return conversation.displayName?.trim() || conversation.email?.trim() || 'Unknown user';
}

function getInitials(name: string) {
  const words = name.trim().split(/\s+/).filter(Boolean);

  return words
    .slice(0, 2)
    .map((word) => word[0])
    .join('')
    .toUpperCase();
}

function getErrorMessage(error: unknown) {
  if (error instanceof ZodError) {
    return error.issues[0]?.message ?? error.message;
  }

  return error instanceof Error ? error.message : 'An unexpected error occurred.';
}

function DuetConversationRow({ conversation }: { conversation: DuetConversationWithPresence }) {
  const partnerName = getPartnerName(conversation);

  return (
    <li className="flex min-w-0 items-center gap-3 px-2 py-3">
      <div className="relative shrink-0">
        {conversation.avatarUrl ? (
          <img
            className="size-11 rounded-full bg-slate-200 object-cover"
            src={conversation.avatarUrl}
            alt=""
            loading="lazy"
            decoding="async"
          />
        ) : (
          <span
            className="grid size-11 place-items-center rounded-full bg-blue-100 text-sm font-bold text-blue-800"
            aria-hidden="true"
          >
            {getInitials(partnerName)}
          </span>
        )}

        <span
          className={cn(
            'absolute right-0 bottom-0 size-3 rounded-full border-2 border-slate-50',
            presenceClasses[conversation.status],
          )}
          role="img"
          aria-label={`Presence: ${conversation.status}`}
        />
      </div>

      <p className="m-0 min-w-0 truncate text-sm font-semibold text-slate-900">{partnerName}</p>
    </li>
  );
}

function DuetConversationListSkeleton() {
  return (
    <div className="grid gap-1" role="status" aria-label="Loading conversations">
      {[0, 1, 2, 3].map((item) => (
        <div className="flex items-center gap-3 px-2 py-3" key={item} aria-hidden="true">
          <span className="size-11 animate-pulse rounded-full bg-slate-200" />
          <span className="h-4 w-32 animate-pulse rounded-full bg-slate-200" />
        </div>
      ))}
    </div>
  );
}

export function DuetConversationList() {
  const { data, error, isError, isFetching, isPending, refetch } = useDuetsWithPresence();

  if (isPending) {
    return <DuetConversationListSkeleton />;
  }

  if (isError) {
    return (
      <div className="grid min-h-48 place-items-center px-4 py-8 text-center" role="alert">
        <div className="grid max-w-56 justify-items-center gap-4">
          <div className="grid gap-2">
            <h2 className="m-0 font-display text-base font-semibold text-slate-900">
              Conversations unavailable
            </h2>
            <p className="m-0 text-sm leading-6 text-slate-500">
              We could not load your conversations.
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

  if (!data.conversations.length) {
    return (
      <div className="grid min-h-48 place-items-center px-4 py-8 text-center">
        <div className="grid max-w-56 justify-items-center gap-3">
          <span className="grid size-11 place-items-center rounded-full bg-blue-50 text-blue-700">
            <MessageCircleMore className="size-5" aria-hidden="true" />
          </span>
          <div className="grid gap-3">
            <h2 className="m-0 font-display text-base font-semibold text-slate-900">
              No conversations yet
            </h2>
            <p className="m-0 text-sm leading-6 text-slate-500">
              Your recent conversations will appear here.
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <ul className="m-0 list-none divide-y divide-slate-200 p-0">
      {data.conversations.map((conversation) => (
        <DuetConversationRow conversation={conversation} key={conversation.conversationId} />
      ))}
    </ul>
  );
}
