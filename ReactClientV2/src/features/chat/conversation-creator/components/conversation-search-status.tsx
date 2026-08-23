import { LoaderCircle } from 'lucide-react';

export function ConversationSearchStatus({ children }: { children: string }) {
  return (
    <div className="grid min-h-32 place-items-center px-4 py-8 text-center" role="status">
      <p className="m-0 max-w-56 text-sm leading-6 text-slate-500">{children}</p>
    </div>
  );
}

export function ConversationSearchLoading() {
  return (
    <div
      className="flex min-h-32 items-center justify-center gap-2 px-4 py-8 text-sm text-slate-500"
      role="status"
    >
      <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />
      Searching users...
    </div>
  );
}
