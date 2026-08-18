import { ArrowLeft, UserPlus } from 'lucide-react';

import { Button } from '@/components/ui/button';

interface ConversationCreatorProps {
  onBack: () => void;
}

export function ConversationCreator({ onBack }: ConversationCreatorProps) {
  return (
    <aside
      className="flex min-h-64 flex-col border-b border-slate-200 bg-slate-50/70 lg:min-h-0 lg:border-r lg:border-b-0"
      aria-labelledby="conversation-creator-heading"
    >
      <div className="border-b border-slate-200 px-5 py-5 sm:px-6">
        <Button className="-ml-3 mb-4 px-3" type="button" variant="ghost" onClick={onBack}>
          <ArrowLeft className="size-4" aria-hidden="true" />
          Back
        </Button>
        <p className="m-0 text-[0.68rem] font-extrabold uppercase tracking-[0.18em] text-blue-700">
          New conversation
        </p>
        <h1
          className="mt-1 mb-0 font-display text-2xl font-semibold tracking-[-0.035em] text-slate-950"
          id="conversation-creator-heading"
        >
          Create a duet
        </h1>
      </div>

      <div className="grid min-h-0 flex-1 place-items-center px-6 py-10 text-center">
        <div className="grid max-w-60 justify-items-center gap-4">
          <span className="grid size-12 place-items-center rounded-full bg-blue-50 text-blue-700">
            <UserPlus className="size-5" aria-hidden="true" />
          </span>
          <p className="m-0 text-sm leading-6 text-slate-600">
            The duet conversation form will be available here soon.
          </p>
        </div>
      </div>
    </aside>
  );
}
