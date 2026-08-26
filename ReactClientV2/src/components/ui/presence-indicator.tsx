import { cn } from '@/utils/cn';

type PresenceIndicatorStatus = 'Active' | 'AFK' | 'Busy' | 'Invisible';

const presenceClasses: Record<PresenceIndicatorStatus, string> = {
  Active: 'bg-emerald-500',
  AFK: 'bg-amber-400',
  Busy: 'bg-red-500',
  Invisible: 'bg-slate-400',
};

interface PresenceIndicatorProps {
  className?: string;
  status: PresenceIndicatorStatus;
}

export function PresenceIndicator({ className, status }: PresenceIndicatorProps) {
  return (
    <span
      className={cn(
        'size-3 rounded-full border-2 border-slate-50',
        presenceClasses[status],
        className,
      )}
      role="img"
      aria-label={`Presence: ${status}`}
    />
  );
}
